[<RequireQualifiedAccess>]
module Database

open System.Data
open Microsoft.Data.Sqlite
open DbUp
open System
open Donald
open Domain
open System.IO

// Source: https://github.com/jacentino/SqlFun/blob/master/SqlFun/Templating.fs
[<RequireQualifiedAccess>]
module private Templating =
  let expandTemplate (placeholder: string) (clause: string) (separator: string) (value: string) (template: string) =
    if template.Contains("{{" + placeholder + "}}") then
      template.Replace("{{" + placeholder + "}}", clause + "{{" + placeholder + "!}}" + value)
    else
      template.Replace("{{" + placeholder + "!}}", "{{" + placeholder + "!}}" + value + separator)

  let cleanUpTemplate (template: string) =
    template.Split([| "{{"; "}}" |], StringSplitOptions.None)
    |> Seq.mapi (fun i s -> if i % 2 = 0 then s else "")
    |> String.concat ""

  let applyWhen condition f = if condition then f else id
  let applyWhenSome value f = applyWhen (Option.isSome value) f
  let applyWhenNone value f = applyWhen (Option.isNone value) f
  let expandWhereClause = expandTemplate "WHERE-CLAUSE" "WHERE " " AND "
  let expandOrderByClause = expandTemplate "ORDER-BY-CLAUSE" "ORDER BY " ", "

type DbConnectionFactory = unit -> IDbConnection

let localDataSource =
  let path = Path.Combine(Environment.CurrentDirectory, "bulletin.db")
  $"Data Source={path}"

let connectionString =
  "SQLITE_DB"
  |> Environment.GetEnvironmentVariable
  |> Option.ofObj
  |> Option.defaultValue localDataSource

let dbConnectionFactory: DbConnectionFactory =
  fun () -> new SqliteConnection(connectionString)

let migrate () =
  DeployChanges.To
    .SQLiteDatabase(connectionString)
    .WithScriptsFromFileSystem("migrations")
    .Build()
    .PerformUpgrade()
  |> ignore<Engine.DatabaseUpgradeResult>

[<RequireQualifiedAccess>]
module FeedType =
  let toDbString feedType =
    match feedType with
    | FeedType.Atom -> "atom"
    | FeedType.Rss RssVersion.V1 -> "rss_v1"
    | FeedType.Rss RssVersion.V2 -> "rss_v2"

  let fromDbString str =
    match str with
    | "atom" -> FeedType.Atom
    | "rss_v1" -> FeedType.Rss RssVersion.V1
    | "rss_v2" -> FeedType.Rss RssVersion.V2
    | _ -> failwith "Invalid feed type string."

let cast<'a> (value: obj) = value :?> 'a

let queryFeeds connection =
  connection
  |> Db.newCommand "SELECT id, name, url, type FROM feeds"
  |> Db.Async.query (fun reader -> {
    Id = reader.ReadInt32 "id"
    Name = reader.ReadString "name"
    Url = reader.ReadString "url"
    Type = FeedType.fromDbString (reader.ReadString "type")
  })

let queryLatestEntryDateTime feedId connection =
  connection
  |> Db.newCommand
    "SELECT updated_at_timestamp
    FROM entries
    WHERE feed_id = @feed_id
    ORDER BY updated_at_timestamp DESC
    LIMIT 1"
  |> Db.setParams [ "feed_id", SqlType.Int feedId ]
  |> Db.Async.querySingle (fun reader -> DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64 "updated_at_timestamp"))

type Paging = { Limit: int; Offset: int }

type EntryQuery = {
  FeedIdQuery: int option
  TitleQuery: string option
  OnlyShowFavorites: bool
  Paging: Paging
}

[<RequireQualifiedAccess>]
module Paging =
  let nextPage paging = {
    paging with
        Offset = paging.Offset + paging.Limit
  }

let queryFeedEntries entryQuery connection =
  connection
  |> Db.newCommand (
    "SELECT 
      feeds.name AS feed_name,
      entries.id,
      entries.feed_id,
      entries.title,
      entries.description,
      entries.is_favorited,
      entries.url,
      entries.published_at_timestamp,
      entries.updated_at_timestamp
    FROM
      entries
      INNER JOIN feeds ON entries.feed_id = feeds.id
      INNER JOIN entries_fts ON entries.id = entries_fts.entry_id
    {{WHERE-CLAUSE}}
    {{ORDER-BY-CLAUSE}}
    LIMIT @limit
    OFFSET @offset"
    |> Templating.applyWhen entryQuery.OnlyShowFavorites (Templating.expandWhereClause "is_favorited = 1")
    |> Templating.applyWhenSome entryQuery.FeedIdQuery (Templating.expandWhereClause "feed_id = @feed_id_query")
    |> Templating.applyWhenNone entryQuery.TitleQuery (Templating.expandOrderByClause "updated_at_timestamp DESC")
    |> Templating.applyWhenSome
      entryQuery.TitleQuery
      (Templating.expandWhereClause "entries_fts MATCH 'title:' || @title_query"
       >> Templating.expandOrderByClause "rank")
    |> Templating.cleanUpTemplate
    |> fun x ->
        printfn "%s" x
        x
  )
  |> Db.setParams [
    "feed_id_query", sqlInt32OrNull entryQuery.FeedIdQuery
    "title_query", sqlStringOrNull entryQuery.TitleQuery
    "limit", SqlType.Int entryQuery.Paging.Limit
    "offset", SqlType.Int entryQuery.Paging.Offset
  ]
  |> Db.Async.query (fun reader ->
    let feedName = reader.ReadString "feed_name"

    let feedEntry = {
      Id = reader.ReadInt32 "id"
      FeedId = reader.ReadInt32 "feed_id"
      Title = reader.ReadString "title"
      Description = reader.ReadStringOption "description"
      IsFavorited = reader.ReadBoolean "is_favorited"
      Url = reader.ReadString "url"
      PublishedAt = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64 "published_at_timestamp")
      UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64 "updated_at_timestamp")
    }

    feedName, feedEntry)

let updateFavorite entryId isFavorited connection =
  connection
  |> Db.newCommand
    "UPDATE entries
    SET is_favorited = @is_favorited
    WHERE id = @id
    RETURNING is_favorited"
  |> Db.setParams [
    "id", SqlType.Int entryId
    "is_favorited", SqlType.Int(if isFavorited then 1 else 0)
  ]
  |> Db.Async.scalar (cast<int64> >> (=) 1)

let insertFeed (feed: Feed) connection =
  connection
  |> Db.newCommand
    "INSERT INTO feeds (name, url, type)
    VALUES (@name, @url, @type)
    ON CONFLICT(url) DO NOTHING
    RETURNING id"
  |> Db.setParams [
    "name", SqlType.String feed.Name
    "url", SqlType.String feed.Url
    "type", SqlType.String(FeedType.toDbString feed.Type)
  ]
  |> Db.Async.scalar (Option.ofObj >> Option.map cast<int64>)

let insertFeedEntry (entry: FeedEntry) connection =
  // TODO: I'm not sure the `publishedDate` in RSS is updated when the same url appears after an 'edit'
  connection
  |> Db.newCommand
    "INSERT INTO entries (feed_id, title, description, is_favorited, url, published_at_timestamp, updated_at_timestamp)
    VALUES (@feed_id, @title, @description, @is_favorited, @url, @published_at_timestamp, @updated_at_timestamp)
    ON CONFLICT(url) DO UPDATE SET updated_at_timestamp = @updated_at_timestamp
    RETURNING id"
  |> Db.setParams [
    "feed_id", SqlType.Int entry.FeedId
    "title", SqlType.String entry.Title
    "description", sqlStringOrNull entry.Description
    "is_favorited", SqlType.Boolean entry.IsFavorited
    "url", SqlType.String entry.Url
    "published_at_timestamp", SqlType.Int64(entry.PublishedAt.ToUnixTimeSeconds())
    "updated_at_timestamp", SqlType.Int64(entry.UpdatedAt.ToUnixTimeSeconds())
  ]
  |> Db.Async.scalar cast<int64>

let deleteFeed feedId connection =
  connection
  |> Db.newCommand
    "DELETE FROM entries WHERE feed_id = @feed_id;
    DELETE FROM feeds WHERE id = @feed_id;
    SELECT changes()"
  |> Db.setParams [ "feed_id", SqlType.Int feedId ]
  |> Db.Async.scalar cast<int64>
