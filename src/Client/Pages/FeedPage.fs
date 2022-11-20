module FeedPage

open System
open Lit
open Lit.Elmish
open Shared

// TODO: Filter by feed.

type State = {
  Model: GetFeedRequest
  Posts: Paginated<PostModel>
  SubscribedFeeds: SubscribedFeed list
}

type Msg =
  | Search
  | SetPage of int
  | SetSearchQuery of string
  | SetOrdering of string
  | SetPosts of Paginated<PostModel>
  | SetSubscribedFeeds of SubscribedFeed list

let getUserFeedFromServerCmd request =
  Elmish.Cmd.OfAsync.perform Remoting.securedServerApi.GetUserFeed request SetPosts

let init () =
  let getPostsModel = {
    Ordering = Newest
    SearchQuery = None
    Feed = None
    Page = 1
    PageSize = 25
  }

  {
    Model = getPostsModel
    Posts = Paginated.empty ()
    SubscribedFeeds = []
  },
  Elmish.Cmd.batch [
    getUserFeedFromServerCmd getPostsModel
    Elmish.Cmd.OfAsync.perform Remoting.securedServerApi.GetSubscribedFeeds () SetSubscribedFeeds
  ]

let update (msg: Msg) (state: State) =
  match msg with
  | SetPosts posts -> { state with Posts = posts }, Elmish.Cmd.none
  | SetSubscribedFeeds feeds -> { state with SubscribedFeeds = feeds }, Elmish.Cmd.none
  | Search ->
    let model = { state.Model with Page = 1 }
    { state with Model = model }, getUserFeedFromServerCmd state.Model
  | SetPage page ->
    if page < 0 || page > state.Posts.PageCount then
      state, Elmish.Cmd.none
    else
      let model = { state.Model with Page = page }
      { state with Model = model }, getUserFeedFromServerCmd model
  | SetSearchQuery query ->
    let query = if String.IsNullOrWhiteSpace query then None else Some query
    let model = { state.Model with SearchQuery = query }
    { state with Model = model }, Elmish.Cmd.none
  | SetOrdering ordering ->
    let ordering =
      match ordering with
      | ordering when ordering = string Ordering.Updated -> Ordering.Updated
      | ordering when ordering = string Ordering.Oldest -> Ordering.Oldest
      | ordering when ordering = string Ordering.Newest -> Ordering.Newest
      | _ -> Ordering.Updated

    let model =
      { state.Model with
          Ordering = ordering
          Page = 1
      }

    { state with Model = model }, getUserFeedFromServerCmd model

[<HookComponent>]
let Post (post: PostModel) =
  html
    $"""
    <div class="border border-black">
      <div>
        <a href={post.Link} target="_blank">
          <h1 class="text-lg hover:underline hover:decoration-solid">
            {post.Title}
          </h1>
        </a>
      </div>
      <div>
        <p class="text-sm">
          Published {post.PublishedAt} ago • Last Updated {post.UpdatedAt} ago • Source: {post.Source}
        </p>
      </div>
    </div>
    """

[<HookComponent>]
let Pagination
  (currentPage: int)
  (numberOfPages: int)
  (hasPrevious: bool)
  (hasNext: bool)
  (goToPrevious: unit -> unit)
  (goToNext: unit -> unit)
  (goToPage: int -> unit)
  =
  let goToPreviousBtn =
    html $"<button @click={fun () -> goToPrevious ()}>Previous</button>"

  let goToNextBtn = html $"<button @click={fun () -> goToNext ()}>Next</button>"

  let paginationBtn isSelected index =
    let classes =
      if isSelected then
        "text-green-700 underline decoration-solid"
      else
        ""

    html
      $"""
      <button class="hover:underline hover:decoration-solid {classes}" @click={fun () -> goToPage index}>{string index}</button>
      """

  html
    $"""
    <div class="flex flex-row gap-x-3 justify-center">
      {if hasPrevious then goToPreviousBtn else Lit.nothing}

      {[
         for i in 1..numberOfPages do
           let isSelected = currentPage = i
           paginationBtn isSelected i
       ]}

      {if hasNext then goToNextBtn else Lit.nothing}
    </div>
    """

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  html
    $"""
    <div>
      <div class="flex flex-row w-full">
        <div class="mr-auto">
          <input placeholder="search query" @change="{EvVal(SetSearchQuery >> dispatch)}" />
          <button @click={Ev(fun _ -> dispatch Search)}>Search</button>
          <select class="ml-3" @change={EvVal(SetOrdering >> dispatch)}>
            <option ?selected={state.Model.Ordering = Ordering.Newest}>{string Ordering.Newest}</option>
            <option ?selected={state.Model.Ordering = Ordering.Updated}>{string Ordering.Updated}</option>
            <option ?selected={state.Model.Ordering = Ordering.Oldest}>{string Ordering.Oldest}</option>
          </select>
        </div>
      </div>
      <div class="flex flex-col items-stretch">
         {state.Posts.Items |> Seq.map Post}
      </div>
      {Pagination
         state.Posts.CurrentPage
         state.Posts.PageCount
         state.Posts.HasPreviousPage
         state.Posts.HasNextPage
         (fun () -> SetPage(state.Model.Page - 1) |> dispatch)
         (fun () -> SetPage(state.Model.Page + 1) |> dispatch)
         (SetPage >> dispatch)}
    </div>
    """
