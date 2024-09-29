module Handlers

open System
open Falco
open Falco.Markup
open Domain
open FsToolkit.ErrorHandling
open System.Net.Http

let viewFeeds: HttpHandler =
  fun ctx ->
    task {
      let connectionFactory = ctx.Plug<Database.DbConnectionFactory>()
      use connection = connectionFactory ()

      let! respondWith =
        connection
        |> Database.queryFeeds
        |> Task.map (Views.feeds >> Views.Templates.page >> Response.ofHtml)

      return! respondWith ctx
    }

let deleteFeed: HttpHandler =
  let readId (requestData: RequestData) = requestData.GetInt32 "id"

  let deletedFeed: HttpHandler = Response.withStatusCode 200 >> Response.ofEmpty

  let nothingWasDeleted: HttpHandler =
    let headers = [ "HX-Reswap", "afterend"; "HX-Retarget", "closest main" ]

    let alert =
      "Oops, Something went wrong! Looks like nothing was deleted :("
      |> Views.Alert.Error
      |> Views.Alert.view

    Response.withStatusCode 200
    >> Response.withHeaders headers
    >> Response.ofHtml alert

  let handleDeletion feedId : HttpHandler =
    fun ctx ->
      task {
        let connectionFactory = ctx.Plug<Database.DbConnectionFactory>()
        use connection = connectionFactory ()

        let! totalRowsDeleted = connection |> Database.deleteFeed feedId

        let respondWith =
          match totalRowsDeleted with
          | 0L -> nothingWasDeleted
          | _ -> deletedFeed

        return! respondWith ctx
      }

  Request.mapRoute readId handleDeletion

let createFeed: HttpHandler =
  let readForm (formData: FormData) = {|
    FeedName = formData.GetString "feed-name"
    FeedUrl = formData.GetString "feed-url"
  |}

  let invalidFeedSource: HttpHandler =
    Response.withStatusCode 500 >> Response.ofPlainText "Invalid Feed"

  let alreadyExists: HttpHandler =
    let headers = [ "HX-Retarget", "closest main"; "HX-Reswap", "afterbegin" ]

    let alert =
      "A feed with that URL already exists." |> Views.Alert.Error |> Views.Alert.view

    Response.withHeaders headers >> Response.ofHtml alert

  let handleForm (form: {| FeedName: string; FeedUrl: string |}) : HttpHandler =
    fun ctx ->
      task {
        let httpClient = ctx.Plug<HttpClient>()
        let connectionFactory = ctx.Plug<Database.DbConnectionFactory>()

        let! feedContent = httpClient.GetStringAsync form.FeedUrl

        match Parser.determineFeedTypeFromXmlString feedContent with
        | None -> return! invalidFeedSource ctx
        | Some feedType ->
          use connection = connectionFactory ()

          let feed = {
            Id = 0
            Name = form.FeedName
            Url = form.FeedUrl
            Type = feedType
          }

          let! feedId = connection |> Database.insertFeed feed

          match feedId with
          | None -> return! alreadyExists ctx
          | Some feedId ->
            let feed = { feed with Id = int feedId }
            let! entries = Parser.getAndParseFeed httpClient DateTimeOffset.UtcNow None feed

            for entry in entries do
              do! connection |> Database.insertFeedEntry entry |> Task.ignore

            return! Response.ofHtml (Views.feed feed) ctx
      }

  Request.mapForm readForm handleForm

let entriesList onlyShowFavorites : HttpHandler =
  let stringToOption value =
    if String.IsNullOrWhiteSpace value then None else Some value

  let concatHtml elements =
    elements |> List.map renderNode |> String.concat "\n"

  let readEntryQuery (requestData: RequestData) : Database.EntryQuery = {
    FeedIdQuery = requestData.TryGetInt "feed"
    TitleQuery = requestData.TryGetString "title" |> Option.map stringToOption |> Option.flatten
    OnlyShowFavorites = onlyShowFavorites
    Paging = {
      Limit = 30
      Offset = requestData.TryGetInt "offset" |> Option.defaultValue 0
    }
  }

  let searchEntries (query: Database.EntryQuery) : HttpHandler =
    fun ctx ->
      task {
        let connectionFactory = ctx.Plug<Database.DbConnectionFactory>()
        use connection = connectionFactory ()

        let now = DateTimeOffset.UtcNow
        let! feeds = connection |> Database.queryFeeds
        let! entries = connection |> Database.queryFeedEntries query

        let pageResponse =
          let headers = Request.getHeaders ctx
          let hxTriggerHeader = headers.GetString "HX-Trigger"
          let requestTriggeredByForm = hxTriggerHeader = "title" || hxTriggerHeader = "feed"

          match headers.GetBoolean "HX-Request" with
          | true when requestTriggeredByForm -> Response.ofHtml (Views.entriesContainer now query entries)
          | true -> Response.ofHtmlString (concatHtml (Views.calculateEntriesView now query entries))
          | _ -> Response.ofHtml (Views.Templates.page (Views.homepage now query feeds entries))

        return! (Response.withHeaders [ "Vary", "HX-Request" ] >> pageResponse) ctx
      }

  Request.mapQuery readEntryQuery searchEntries

let updateFavorite isFavorited : HttpHandler =
  let getEntryId (data: RequestData) = data.GetInt "id"

  let handle entryId : HttpHandler =
    fun ctx ->
      task {
        let connectionFactory = ctx.Plug<Database.DbConnectionFactory>()
        use connection = connectionFactory ()
        let! isFavorited = connection |> Database.updateFavorite entryId isFavorited
        return! Response.ofHtml (Views.favoriteIcon entryId isFavorited) ctx
      }

  Request.mapRoute getEntryId handle
