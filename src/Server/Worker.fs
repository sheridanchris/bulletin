module Worker

open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open FSharp.Data
open System
open Data
open FsToolkit.ErrorHandling
open Marten
open DataAccess
open Microsoft.Extensions.DependencyInjection

type RSS = XmlProvider<"http://rss.cnn.com/rss/cnn_topstories.rss">

let pollingTimespan = TimeSpan.FromMinutes(30)

let toDateTime (offset: DateTimeOffset) = offset.UtcDateTime

let toPost (shortName: string) (item: RSS.Item) = {
  Id = Guid.NewGuid()
  Headline = item.Title
  Published = item.PubDate |> Option.defaultValue DateTimeOffset.UtcNow |> toDateTime
  Link = item.Link
  AuthorName = None
  Score = 0
  FeedName = shortName
}

let filterSource (latest: DateTime option) (post: Post) =
  match latest with
  | None -> true
  | Some latest -> post.Published > latest

let readSourceAsync (latest: DateTime option) (source: NewsSource) = task {
  let! rssResult = RSS.AsyncLoad(source.RssFeed) |> Async.Catch

  return
    match rssResult with
    | Choice1Of2 rss ->
      rss.Channel.Items
      |> Array.distinctBy (fun item -> item.Link)
      |> Array.map (toPost source.ShortName)
      |> Array.filter (filterSource latest)
      |> Array.toList
    | Choice2Of2 ex ->
      // TODO: I need to do proper logging.
      printfn "An exception has occured: %s" ex.StackTrace
      []
}

type IScopedBackgroundService =
  abstract member DoWorkAsync: CancellationToken -> Task

type RssWorker(querySession: IQuerySession, documentSession: IDocumentSession) =
  interface IScopedBackgroundService with
    member _.DoWorkAsync(stoppingToken: CancellationToken) = task {
      while not stoppingToken.IsCancellationRequested do
        let! sources = querySession |> getSourcesAsync stoppingToken

        let! latestPost = querySession |> latestPostAsync stoppingToken
        let! results = sources |> Seq.map (readSourceAsync latestPost) |> Task.WhenAll

        let posts =
          results
          |> Array.toList
          |> List.concat
          |> List.distinctBy (fun post -> post.Link)

        let links = [| for post in posts -> post.Link |]

        let! urlsInDb =
          querySession
          |> findPostsByUrls links stoppingToken
          |> Task.map (Seq.map (fun post -> post.Link))

        // TODO: instead of filtering out the post, I should probably set it to `updated`.
        let distinctPosts =
          posts |> List.filter (fun post -> not (Seq.contains post.Link urlsInDb))

        documentSession |> Session.storeMany distinctPosts
        do! documentSession |> Session.saveChangesTask stoppingToken
        do! Task.Delay(pollingTimespan)
    }

type RssWorkerBackgroundService(serviceProvider: IServiceProvider) =
  inherit BackgroundService()

  override _.ExecuteAsync(stoppingToken: CancellationToken) : Task = task {
    use scope = serviceProvider.CreateScope()
    let worker = scope.ServiceProvider.GetService<IScopedBackgroundService>()
    do! worker.DoWorkAsync(stoppingToken)
  }
