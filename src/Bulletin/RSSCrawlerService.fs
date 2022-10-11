module Crawler

open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open FSharp.Data
open System
open Domain

type RSS = XmlProvider<"http://rss.cnn.com/rss/cnn_topstories.rss">

let pollingTimespan = TimeSpan.FromSeconds(15) // development.

let sources =
    [ "http://rss.cnn.com/rss/cnn_topstories.rss"
      "https://feeds.nbcnews.com/nbcnews/public/news" ]

let toPost (item: RSS.Item) =
    { Id = 0
      Headline = item.Title
      Description = item.Description
      Link = item.Link
      Poster = None
      PublishedDate = item.PubDate
      Votes = [] }

let readSourceAsync (source: string) =
    task {
        let! rss = RSS.AsyncLoad(source)
        return rss.Channel.Items |> Array.map toPost |> Array.toList
    }

type RSSCrawlerService() =
    interface IHostedService with
        member _.StartAsync(cancellationToken: CancellationToken): Task =
            task {
                let! results = sources |> List.map readSourceAsync |> Task.WhenAll
                for posts in results do
                    do! Persistence.insertPosts posts |> Persistence.runAsync
            }

        member _.StopAsync(cancellationToken: CancellationToken): Task = 
            failwith "Not Implemented"