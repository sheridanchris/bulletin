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
open FSharp.UMX

type RSS = XmlProvider<"http://rss.cnn.com/rss/cnn_topstories.rss">

let pollingTimespan = TimeSpan.FromMinutes(30)

let toDateTime (offset: DateTimeOffset) = offset.UtcDateTime

let toPost (feed: RssFeed) (item: RSS.Item) =
  let published =
    item.PubDate |> Option.map toDateTime |> Option.defaultValue DateTime.UtcNow

  {
    Id = % Guid.NewGuid()
    Headline = item.Title
    PublishedAt = published
    LastUpdatedAt = published
    Link = item.Link
    Feed = feed.Id
  }

let filterSource (lastUpdatedAt: DateTime option) (post: Post) =
  match lastUpdatedAt with
  | None -> true
  | Some lastUpdatedAt -> post.LastUpdatedAt > lastUpdatedAt

let readSourceAsync (latest: DateTime option) (feed: RssFeed) = task {
  let! rssResult = RSS.AsyncLoad(feed.RssFeedUrl) |> Async.Catch

  return
    match rssResult with
    | Choice1Of2 rss ->
      rss.Channel.Items
      |> Array.map (toPost feed)
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
        let! sources = querySession |> getRssFeeds

        let! latestPost = querySession |> latestPostAsync
        let! results = sources |> Seq.map (readSourceAsync latestPost) |> Task.WhenAll

        let posts =
          results
          |> Array.toList
          |> List.concat
          |> List.distinctBy (fun post -> post.Link)

        let! postsInDb = querySession |> findPostsByUrls [| for post in posts -> post.Link |]

        let updatedPosts =
          posts
          |> List.map (fun post ->
            match postsInDb |> Seq.tryFind (fun p -> p.Link = post.Link) with
            | Some post -> { post with LastUpdatedAt = DateTime.UtcNow } // TODO: Idk if this DateTime is correct?
            | None -> post)

        documentSession |> Session.storeMany updatedPosts
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
