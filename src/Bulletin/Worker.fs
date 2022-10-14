module Worker

open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open FSharp.Data
open System
open Domain
open FsToolkit.ErrorHandling
open Persistence

type RSS = XmlProvider<"http://rss.cnn.com/rss/cnn_topstories.rss">

let pollingTimespan = TimeSpan.FromMinutes(30)

let sources = [
    "http://rss.cnn.com/rss/cnn_topstories.rss"
    "https://feeds.nbcnews.com/nbcnews/public/news"
    "https://rss.nytimes.com/services/xml/rss/nyt/World.xml"
    "https://globalnews.ca/feed"
]

let toDateTime (offset: DateTimeOffset) = offset.UtcDateTime

let toPost (item: RSS.Item) = {
    Id = Guid.NewGuid()
    Headline = item.Title
    Link = item.Link
    PosterId = None
    PublishedDate = toDateTime item.PubDate.Value // this is filtered out. maybe I should be safe??? idk
}

let filterSource (latest: DateTime option) (post: Post) =
    match latest with
    | None -> true
    | Some latest -> post.PublishedDate > latest

let readSourceAsync (latest: DateTime option) (source: string) = task {
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

type RssWorker(connectionFactory: DbConnectionFactory) =
    inherit BackgroundService()

    override _.ExecuteAsync(stoppingToken: CancellationToken) : Task = task {
        while not (stoppingToken.IsCancellationRequested) do
            use connection = connectionFactory ()

            let! latestPost =
                connection
                |> getLatestPostAsync
                |> Task.map (fun dates -> dates |> Seq.tryHead |> Option.map (fun item -> item.PublishedDate))

            let! results = sources |> List.map (readSourceAsync latestPost) |> Task.WhenAll

            let posts =
                results
                |> Array.toList
                |> List.filter (fun posts -> not (List.isEmpty posts))
                |> List.collect id

            if not (List.isEmpty posts) then
                let! _ = connection |> insertPostsAsync posts
                ()

            do! Task.Delay(pollingTimespan)
    }
