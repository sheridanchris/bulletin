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

let sources =
    [ "http://rss.cnn.com/rss/cnn_topstories.rss"
      "https://feeds.nbcnews.com/nbcnews/public/news"
      "https://globalnews.ca/feed" ]

let toDateTime (offset: DateTimeOffset) = offset.UtcDateTime

let toPost (item: RSS.Item) =
    { Id = Guid.NewGuid()
      Headline = item.Title
      Published = toDateTime item.PubDate.Value
      Link = item.Link
      AuthorName = None
      Votes = []
      Score = 0 }

let filterSource (latest: DateTime option) (post: Post) =
    match latest with
    | None -> true
    | Some latest -> post.Published > latest

let readSourceAsync (latest: DateTime option) (source: string) =
    task {
        let! rssResult = RSS.AsyncLoad(source) |> Async.Catch

        return
            match rssResult with
            | Choice1Of2 rss ->
                rss.Channel.Items
                |> Array.distinctBy (fun item -> item.Guid) // idk?
                |> Array.filter (fun item -> Option.isSome item.PubDate)
                |> Array.map toPost
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
        member _.DoWorkAsync(stoppingToken: CancellationToken) =
            task {
                while not (stoppingToken.IsCancellationRequested) do
                    let! latestPost = querySession |> latestPostAsync stoppingToken
                    let! results = sources |> List.map (readSourceAsync latestPost) |> Task.WhenAll

                    let posts =
                        results
                        |> Array.toList
                        |> List.filter (fun posts -> not (List.isEmpty posts))
                        |> List.collect id

                    let urls = posts |> List.map (fun post -> post.Link) |> ResizeArray
                    let! urlsInDb = querySession |> findPostsByUrls urls stoppingToken

                    let distinctPosts =
                        posts |> List.filter (fun post -> not (List.contains post.Link urlsInDb))

                    documentSession |> Session.storeMany distinctPosts
                    do! documentSession |> Session.saveChangesTask stoppingToken
                    do! Task.Delay(pollingTimespan)
            }

type RssWorkerBackgroundService(serviceProvider: IServiceProvider) =
    inherit BackgroundService()

    override _.ExecuteAsync(stoppingToken: CancellationToken) : Task =
        task {
            use scope = serviceProvider.CreateScope()
            let worker = scope.ServiceProvider.GetService<IScopedBackgroundService>()
            do! worker.DoWorkAsync(stoppingToken)
        }
