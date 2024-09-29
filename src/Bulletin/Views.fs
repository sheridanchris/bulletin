[<RequireQualifiedAccess>]
module Views

open System
open Falco.Markup
open Falco.Markup.Attr
open Falco.Markup.Elem
open Humanizer
open Domain

[<AutoOpen>]
module Hx =
  let hxGet = Attr.create "hx-get"
  let hxDelete = Attr.create "hx-delete"
  let hxPost = Attr.create "hx-post"
  let hxPut = Attr.create "hx-put"
  let hxTrigger = Attr.create "hx-trigger"
  let hxSwap = Attr.create "hx-swap"
  let hxTarget = Attr.create "hx-target"
  let hxInclude = Attr.create "hx-include"

  let hxPushUrl (value: bool) =
    Attr.create "hx-push-url" (value.ToString().ToLower())

[<AutoOpen>]
module Attr =
  let classes values = values |> String.concat " " |> class'

[<AutoOpen>]
module Elem =
  let button attributes children =
    button
      [
        classes [ "py-3"; "px-4"; "border"; "rounded-lg"; "text-white"; "bg-blue-600" ]
        yield! attributes
      ]
      children

  let input attributes =
    input [
      classes [
        "py-3"
        "px-4"
        "border"
        "rounded-lg"
        "border-gray-200"
        "focus:border-blue-500"
        "focus:ring-blue-500"
      ]
      yield! attributes
    ]

  let select attributes children =
    select
      [
        classes [
          "py-3"
          "px-4"
          "block"
          "w-full"
          "border-gray-200"
          "rounded-lg"
          "focus:border-blue-500"
          "focus:ring-blue-500"
        ]
        yield! attributes
      ]
      children

[<RequireQualifiedAccess>]
module Templates =
  let page body =
    Templates.html5 "en" [
      link [ rel "stylesheet"; href "tailwind.css" ]
      script [
        src "https://unpkg.com/htmx.org@2.0.2"
        integrity "sha384-Y7hw+L/jvKeWIRRkqWYfPcvVxHzVzn5REgzbawhxAuQGwX1XWe70vji+VSeHOThJ"
        crossorigin "anonymous"
      ] []
      script [ src "https://kit.fontawesome.com/6d3af4481a.js"; crossorigin "anonymous" ] []
    ] [
      header [ classes [ "w-100"; "py-4"; "px-4" ] ] [
        nav [ classes [ "flex"; "flex-row"; "justify-between"; "items-center" ] ] [
          ul [] [ li [] [ p [] [ Text.raw "Bulletin" ] ] ]
          ul [ classes [ "flex"; "flex-row"; "gap-x-4" ] ] [
            li [] [ a [ href "/" ] [ Text.raw "Posts" ] ]
            li [] [ a [ href "/favorites" ] [ Text.raw "Favorites" ] ]
            li [] [ a [ href "/feeds" ] [ Text.raw "Feeds" ] ]
          ]
        ]
      ]

      yield! body
    ]

[<RequireQualifiedAccess>]
type Alert =
  | Success of string
  | Error of string

[<RequireQualifiedAccess>]
module Alert =
  let view alert =
    let className, message =
      match alert with
      | Alert.Success message -> "is-success", message
      | Alert.Error message -> "is-danger", message

    div [ class' $"notification {className}" ] [ Text.raw message ]

let feed (feed: Feed) =
  div [
    classes [
      "w-full"
      "flex"
      "flex-row"
      "justify-between"
      "shadow-xl"
      "py-10"
      "px-4"
    ]
  ] [
    div [] [ a [ targetBlank; href feed.Url ] [ Text.raw feed.Name ] ]
    i [
      hxDelete $"/feeds/{feed.Id}"
      hxTarget "closest div"
      hxSwap "delete"
      classes [ "fa-solid"; "fa-trash" ]
    ] []
  ]

let feeds feeds = [
  main [ classes [ "container mx-auto" ] ] [
    form [
      hxPost "/feeds"
      hxTarget "#feeds"
      hxSwap "afterend"
      classes [ "flex"; "flex-row"; "items-center"; "justify-center"; "gap-x-2" ]
    ] [
      p [ classes [ "flex"; "flex-col"; "gap-y-2" ] ] [
        label [ for' "feed-name" ] [ Text.raw "Feed Name" ]
        input [ id "feed-name"; name "feed-name"; placeholder "acme, inc" ]
      ]
      p [ classes [ "flex"; "flex-col"; "gap-y-2" ] ] [
        label [ for' "feed-url" ] [ Text.raw "Feed Url" ]
        input [ id "feed-url"; name "feed-url"; placeholder "http://site.com/feed.rss" ]
      ]
      p [ classes [ "mt-auto" ] ] [ button [ type' "submit" ] [ Text.raw "Subscribe" ] ]
    ]
    section [ id "feeds" ] [ h1 [] [ Text.raw "Feeds" ]; yield! List.map feed feeds ]
  ]
]

let favoriteIcon entryId isFavorited =
  i [
    hxPut (
      if isFavorited then
        $"/unfavorite/{entryId}"
      else
        $"/favorite/{entryId}"
    )
    hxTarget "this"
    hxSwap "outerHTML"

    class' (
      if isFavorited then
        "fa-solid fa-star"
      else
        "fa-regular fa-star"
    )
  ] []

let calculateEntriesView (now: DateTimeOffset) (query: Database.EntryQuery) (entries: (string * FeedEntry) list) =
  let feedParam = query.FeedIdQuery |> Option.map string |> Option.defaultValue ""
  let titleParam = query.TitleQuery |> Option.defaultValue ""
  let offsetParam = query.Paging |> Database.Paging.nextPage |> _.Offset

  entries
  |> List.indexed
  |> List.map (fun (index, (feedName, entry)) ->
    div [
      classes [
        "w-full"
        "flex"
        "flex-row"
        "items-center"
        "gap-x-2"
        "shadow"
        "py-3"
        "px-4"
        "bg-gray-200"
      ]

      if index = entries.Length - 1 then
        hxGet (
          (if query.OnlyShowFavorites then "/favorites" else "/")
          + $"?feed={feedParam}&title={titleParam}&offset={offsetParam}"
        )

        hxTrigger "revealed"
        hxSwap "beforeend"
        hxTarget "#entries"
    ] [
      favoriteIcon entry.Id entry.IsFavorited

      div [] [
        a [ classes [ "hover:underline"; "text-xl" ]; href entry.Url; target "_blank" ] [ Text.raw entry.Title ]

        match entry.Description with
        | None -> ()
        | Some description -> p [ classes [ "text-sm" ] ] [ Text.raw description ]

        p [ classes [ "text-xs" ] ] [
          let comparisonDateTime = Operators.max entry.PublishedAt entry.UpdatedAt
          let difference = now - comparisonDateTime
          let humanDifference = difference.Humanize()
          Text.raw $"Updated {humanDifference} ago"
        ]
      ]

      div [ classes [ "ml-auto" ] ] [ p [] [ Text.raw feedName ] ]
    ])

let entriesContainer now query entries =
  entries
  |> calculateEntriesView now query
  |> div [ id "entries"; classes [ "flex"; "flex-col"; "gap-y-2" ] ]

let homepage now (query: Database.EntryQuery) (feeds: Feed list) (entries: (string * FeedEntry) list) = [
  main [ classes [ "container"; "mx-auto"; "flex"; "flex-col"; "gap-y-2" ] ] [
    form [ classes [ "flex"; "flex-row"; "justify-center"; "gap-x-4" ] ] [
      p [ classes [ "flex"; "flex-col" ] ] [
        label [ for' "feed" ] [ Text.raw "Feed" ]
        select [
          id "feed"
          name "feed"
          hxGet (if query.OnlyShowFavorites then "/favorites" else "/")
          hxTarget "#entries"
          hxInclude "closest form"
          hxSwap "outerHTML"
          hxPushUrl true
        ] [
          option [
            value "all"
            if query.FeedIdQuery = None then
              selected
          ] [ Text.raw "All Feeds" ]
          for feed in feeds do
            option [
              if query.FeedIdQuery = Some feed.Id then
                selected
              value (string feed.Id)
            ] [ Text.raw feed.Name ]
        ]
      ]
      p [ classes [ "flex"; "flex-col" ] ] [
        label [ for' "title" ] [ Text.raw "Title" ]
        input [
          id "title"
          name "title"
          hxGet (if query.OnlyShowFavorites then "/favorites" else "/")
          hxInclude "closest form"
          hxTrigger "input changed delay:500ms, title"
          hxTarget "#entries"
          hxSwap "outerHtml"
          hxPushUrl true
        ]
      ]
    ]
    entriesContainer now query entries
  ]
]
