module App

open Fable.Core
open Fable.React
open Feliz
open Elmish
open Shared

type State = {
  Model: GetPostsModel
  Posts: Paginated<PostModel>
}

type Msg =
  | Search
  | SetPage of int
  | SetSearchQuery of string
  | SetOrdering of Ordering
  | GetPostsFromServer of Paginated<PostModel>

let getPostsFromServerCmd model =
  Cmd.OfAsync.perform Remoting.serverApi.GetPosts model GetPostsFromServer

let init () =
  let getPostsModel = {
    Ordering = Latest
    SearchQuery = ""
    Page = 1
    PageSize = 25
  }

  {
    Model = getPostsModel
    Posts = Paginated.empty ()
  },
  getPostsFromServerCmd getPostsModel

let update (msg: Msg) (state: State) =
  match msg with
  | Search -> state, getPostsFromServerCmd state.Model
  | SetPage page ->
    if page < 0 || page > state.Posts.PageCount then
      state, Cmd.none
    else
      let model = { state.Model with Page = page }
      { state with Model = model }, getPostsFromServerCmd model
  | SetSearchQuery query ->
    let model = { state.Model with SearchQuery = query }
    { state with Model = model }, Cmd.none
  | SetOrdering ordering ->
    let model = { state.Model with Ordering = ordering }
    { state with Model = model }, getPostsFromServerCmd model
  | GetPostsFromServer posts -> { state with Posts = posts }, Cmd.none

[<JSX.Component>]
let Post (post: PostModel) =
  let author = post.Author |> Option.defaultValue "automated bot"

  let upvoteClasses =
    if post.Upvoted then
      "hover:text-black text-lime-400"
    else
      "hover:text-lime-400 text-black"

  let downvoteClasses =
    if post.Downvoted then
      "hover:text-black text-rose-600"
    else
      "hover:text-rose-600 text-black"

  Html.div [
    prop.className "border border-black"
    prop.children [
      Html.div [
        prop.className "flex flex-row gap-x-2"
        prop.children [
          Html.div [
            prop.className "flex flex-col justify-items-center"
            prop.children [
              Html.button [
                prop.text "▲"
                prop.className upvoteClasses
              ]
              Html.button [
                prop.text "▼"
                prop.className downvoteClasses
              ]
            ]
          ]
          Html.div [
            Html.div [
              Html.a [
                prop.href post.Link
                prop.target "_blank"
                prop.children [
                  Html.h1 [
                    prop.className "text-lg hover:underline hover:decoration-solid"
                    prop.text post.Title
                  ]
                ]
              ]
            ]
            Html.div [
              Html.p [
                prop.className "text-sm"
                prop.text
                  $"{post.Score} votes • Posted by {post.Author |> Option.defaultValue author} {post.PublishedAt} ago • 0 comments"
              ]
            ]
          ]
        ]
      ]
    ]
  ]

[<JSX.Component>]
let Component () =
  let state, dispatch = React.useElmish (init, update)

  Html.div [
    Html.div [
      prop.className "flex flex-row w-full"
      prop.children [
        Html.div [
          prop.className "mr-auto"
          prop.children [
            Html.input [
              prop.placeholder "search query"
              prop.onTextChange (SetSearchQuery >> dispatch)
            ]
            Html.button [
              prop.onClick (fun _ -> dispatch Search)
              prop.text "Search"
            ]
          ]
        ]
        Html.div [
          prop.className "flex ml-auto mr-2 gap-x-2"
          prop.children [
            Html.button [
              prop.text "Top"
              prop.className (
                if state.Model.Ordering = Ordering.Top then
                  "text-green-700 underline decoration-solid"
                else
                  ""
              )
              prop.onClick (fun _ -> dispatch (SetOrdering Ordering.Top))
            ]
            Html.button [
              prop.text "Latest"
              prop.className (
                if state.Model.Ordering = Ordering.Latest then
                  "text-green-700 underline decoration-solid"
                else
                  ""
              )
              prop.onClick (fun _ -> dispatch (SetOrdering Ordering.Latest))
            ]
            Html.button [
              prop.text "Oldest"
              prop.className (
                if state.Model.Ordering = Ordering.Oldest then
                  "text-green-700 underline decoration-solid"
                else
                  ""
              )
              prop.onClick (fun _ -> dispatch (SetOrdering Ordering.Oldest))
            ]
          ]
        ]
      ]
    ]
    Html.div [
      prop.className "flex flex-col items-stretch"
      prop.children (state.Posts.Items |> Seq.map Post)
    ]
    Html.div [
      prop.className "flex flex-row gap-x-3 justify-center"
      prop.children [
        if state.Posts.HasPreviousPage then
          yield
            Html.button [
              prop.text "Previous"
              prop.onClick (fun _ -> dispatch (SetPage(state.Posts.CurrentPage - 1)))
            ]

        for i in 1 .. state.Posts.PageCount do
          let isSelected = state.Posts.CurrentPage = i

          let classes =
            if isSelected then
              "text-green-700 underline decoration-solid"
            else
              ""

          yield
            Html.button [
              prop.text $"{i}"
              prop.onClick (fun _ -> dispatch (SetPage i))
              prop.className $"hover:underline hover:decoration-solid {classes}"
            ]

        if state.Posts.HasNextPage then
          yield
            Html.button [
              prop.text "Next"
              prop.onClick (fun _ -> dispatch (SetPage(state.Posts.CurrentPage + 1)))
            ]
      ]
    ]
  ]
