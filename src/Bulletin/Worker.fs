module Worker

open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open FSharp.Data
open System
open Domain
open Microsoft.Extensions.Configuration
open Npgsql
open Persistence

// TODO: How are we going to ignore duplicates?
type RSS = XmlProvider<"http://rss.cnn.com/rss/cnn_topstories.rss">

let pollingTimespan = TimeSpan.FromMinutes(30)

let sources =
    [ "http://rss.cnn.com/rss/cnn_topstories.rss"
      "https://feeds.nbcnews.com/nbcnews/public/news" ]

let toPost (item: RSS.Item) =
    { Id = Guid.NewGuid()
      Headline = item.Title
      Link = item.Link
      PosterId = None
      PublishedDate = DateTime.UtcNow }

let readSourceAsync (source: string) =
    task {
        let! rss = RSS.AsyncLoad(source)
        return rss.Channel.Items |> Array.map toPost |> Array.toList
    }

type RssWorker(connectionFactory: DbConnectionFactory) =
    inherit BackgroundService()
    with
        override _.ExecuteAsync(stoppingToken: CancellationToken) : Task =
            task {
                while not (stoppingToken.IsCancellationRequested) do
                    use connection = connectionFactory ()
                    let! results = sources |> List.map readSourceAsync |> Task.WhenAll

                    for posts in results do
                        let! _ = connection |> insertPostsAsync posts
                        ()

                    do! Task.Delay(pollingTimespan)
            }
