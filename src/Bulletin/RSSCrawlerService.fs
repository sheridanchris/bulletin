module Crawler

open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open FSharp.Data
open System

type RSS = XmlProvider<"http://rss.cnn.com/rss/cnn_topstories.rss">

let pollingTimespan = TimeSpan.FromSeconds(15) // development.

let sources =
    [ "http://rss.cnn.com/rss/cnn_topstories.rss"
      "https://feeds.nbcnews.com/nbcnews/public/news"
      "https://moxie.foxnews.com/google-publisher/latest.xml" ]

type RSSCrawlerService() =
    interface IHostedService with
        member _.StartAsync(cancellationToken: CancellationToken): Task =
            failwith "Not Implemented"

        member _.StopAsync(cancellationToken: CancellationToken): Task = 
            failwith "Not Implemented"