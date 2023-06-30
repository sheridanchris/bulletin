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
open FsLibLog
open FsLibLog.Types

(*
  TODO:
  Need to figure out how to support more feeds (rss/atom) ...
  Some rss feeds simply do not work with this schema.
*)

[<Literal>]
let rssFeedSample =
  """
  <rss>
    <channel>
      <item>
        <title>Title</title>
        <link>Link</link>
        <pubDate>Sun, 11 Dec 2022 19:10:01 GMT</pubDate>
      </item>
      <item>
        <title>Title</title>
        <link>Link</link>
      </item>
    </channel>
  </rss>
  """

type RSS = XmlProvider<rssFeedSample>

let pollingTimespan = TimeSpan.FromMinutes(30)

let toDateTime (offset: DateTimeOffset) = offset.UtcDateTime

// Sometimes the RSS feed doesn't match the schema defined by the type provider.
// This will cause an exception, so this function returns an Optional value if that's the case.
let toPost (feed: RssFeed) (item: RSS.Item) =
  try
    let published =
      item.PubDate |> Option.map toDateTime |> Option.defaultValue DateTime.UtcNow

    Some {
      Id = % Guid.NewGuid()
      Headline = item.Title
      PublishedAt = published
      LastUpdatedAt = published
      Link = item.Link
      Feed = feed.Id
    }
  with ex ->
    logger.error (
      Log.setMessage "An exception has occured when converting an RSS item to a post for feed: {feedId}"
      >> Log.addParameter feed.Id
      >> Log.addExn ex
    )

    None

let filterSource (lastUpdatedAt: DateTime option) (post: Post) =
  match lastUpdatedAt with
  | None -> true
  | Some lastUpdatedAt -> post.LastUpdatedAt > lastUpdatedAt

let readSourceAsync (latest: DateTime option) (feed: RssFeed) =
  task {
    let! rssResult = RSS.AsyncLoad(feed.RssFeedUrl) |> Async.Catch

    return
      match rssResult with
      | Choice1Of2 rss ->
        rss.Channel.Items
        |> Array.map (toPost feed)
        |> Array.choose id
        |> Array.filter (filterSource latest)
        |> Array.toList
      | Choice2Of2 ex ->
        logger.error (
          Log.setMessage "An exception has occured when reading an rss feed. Id: {feedId}"
          >> Log.addParameter feed.Id
          >> Log.addExn ex
        )

        []
  }

type Work = CancellationToken -> Task

let asyncWork
  (getRssFeeds: GetRssFeeds)
  (getLatestPostAsync: GetLatestPostAsync)
  (findPostsByUrlsAsync: FindPostsByUrls)
  (savePostsAsync: SaveManyAsync<Post>)
  : Work =
  fun stoppingToken ->
    task {
      while not stoppingToken.IsCancellationRequested do
        let! sources = getRssFeeds ()

        let! latestPost = getLatestPostAsync ()
        let! results = sources |> Seq.map (readSourceAsync latestPost) |> Task.WhenAll

        let posts =
          results
          |> Array.toList
          |> List.concat
          |> List.distinctBy (fun post -> post.Link)

        let! postsInDb = findPostsByUrlsAsync [| for post in posts -> post.Link |]

        let updatedPosts =
          posts
          |> List.map (fun post ->
            // This **should** be the 'UpdatedAt' DateTime for already published posts
            let published = post.PublishedAt

            match postsInDb |> Seq.tryFind (fun p -> p.Link = post.Link) with
            | Some post -> { post with LastUpdatedAt = published }
            | None -> post)

        do! savePostsAsync updatedPosts
        do! Task.Delay(pollingTimespan, stoppingToken)
    }

type RssWorkerBackgroundService(serviceProvider: IServiceProvider) =
  inherit BackgroundService()

  override _.ExecuteAsync(stoppingToken: CancellationToken) : Task =
    task {
      use scope = serviceProvider.CreateScope()

      let querySession = scope.ServiceProvider.GetRequiredService<IQuerySession>()
      let documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>()

      let asyncWork =
        asyncWork
          (getRssFeeds querySession)
          (latestPostAsync querySession)
          (findPostsByUrls querySession)
          (saveManyAsync documentSession)

      do! asyncWork stoppingToken
    }
