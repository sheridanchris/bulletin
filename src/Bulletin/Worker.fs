module Worker

open System.Net.Http
open Microsoft.Extensions.Hosting
open Domain
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open System

let refreshInterval = TimeSpan.FromMinutes 30

type FeedReaderService(httpClient: HttpClient, dbConnectionFactory: Database.DbConnectionFactory) =
  inherit BackgroundService()

  override _.ExecuteAsync stoppingToken =
    task {
      while not stoppingToken.IsCancellationRequested do
        use connection = dbConnectionFactory ()

        let utcNow = DateTimeOffset.UtcNow
        let! feeds = connection |> Database.queryFeeds

        for feed in feeds do
          let! latestEntry = connection |> Database.queryLatestEntryDateTime feed.Id
          let! entries = Parser.getAndParseFeed httpClient utcNow latestEntry feed

          for entry in entries do
            do! connection |> Database.insertFeedEntry entry |> Task.ignore

        do! Task.Delay refreshInterval
    }
