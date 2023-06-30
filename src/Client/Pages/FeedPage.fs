module FeedPage

open System
open Fable
open Lit
open Lit.Elmish
open LitStore
open Shared
open Thoth.Elmish

type State = {
  Debouncer: Debouncer.State
  SearchQuery: string
}

type Msg =
  | DebouncerSelfMsg of Debouncer.SelfMessage<Msg>
  | SetSearchQuery of string
  | EndOfInput

let init () =
  {
    Debouncer = Debouncer.create ()
    SearchQuery = ""
  },
  Elmish.Cmd.none

let update msg state =
  match msg with
  | DebouncerSelfMsg debouncerMsg ->
    let debouncerModel, debouncerCmd = Debouncer.update debouncerMsg state.Debouncer

    {
      state with
          Debouncer = debouncerModel
    },
    debouncerCmd
  | SetSearchQuery query ->
    let debouncerModel, debouncerCmd =
      state.Debouncer
      |> Debouncer.bounce (TimeSpan.FromMilliseconds 750) "search_query" EndOfInput

    {
      state with
          SearchQuery = query
          Debouncer = debouncerModel
    },
    Elmish.Cmd.batch [ Elmish.Cmd.map DebouncerSelfMsg debouncerCmd ]
  | EndOfInput ->
    state, Elmish.Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.SetSearchQuery state.SearchQuery))

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
  let pageButton (page: int) (isSelected: bool) =
    html
      $"""
        <button
          @click={Ev(fun _ -> goToPage page)}
          class="{if isSelected then
                    "join-item btn btn-active"
                  else
                    "join-item btn"}"
        >
        {page}
        </button>
      """

  let previousButton () =
    html
      $"""
      <button class="join-item btn" @click={Ev(fun _ -> goToPrevious ())}>Previous Page</button>
      """

  let nextButton () =
    html
      $"""
      <button class="join-item btn" @click={Ev(fun _ -> goToNext ())}>Next Page</button>
      """

  html
    $"""
    <div class="join">
      {[
         if hasPrevious then
           previousButton ()

         for i in 1..numberOfPages do
           let selected = currentPage = i
           pageButton i selected

         if hasNext then
           nextButton ()
       ]}
    </div>
    """

[<HookComponent>]
let FeedSelector (selectedFeed: FeedId option) (feeds: SubscribedFeed list) (dispatch: ApplicationContext.Msg -> unit) =
  let option (feed: SubscribedFeed) =
    html
      $"""
      <option value={string feed.FeedId} ?selected={Some feed.FeedId = selectedFeed}>{feed.Name}</option>
      """

  html
    $"""
    <select @change={EvVal(ApplicationContext.SetSelectedFeed >> dispatch)} id="feed" class="select">
      <option ?selected={selectedFeed = None} value="none">All Feeds</option>
      {feeds |> List.map option}
    </select>
    """

[<HookComponent>]
let Component () =
  let store = Hook.useStore ApplicationContext.store
  let state, dispatch = Hook.useElmish (init, update)

  let displayPostsInRegularMode posts =
    let displayPost post =
      html
        $"""
        <li class="py-3 sm:py-4">
          <div class="flex items-center space-x-4">
            <div class="flex-1 min-w-0">
              <a class="link link-hover" href={post.Link} target="_blank">{post.Title}</a>
              <p class="label-text">Published {post.PublishedAt} ago · Last Updated {post.UpdatedAt} ago</p>
            </div>
            <span class="inline-flex items-center text-base font-semibold label-text">{post.Source}</span>
          </div>
        </li>
        """

    html
      $"""
      <div class="card card-bordered bg-base-100 shadow-xl">
        <div class="card-body">
          <ul role="list" class="divide-y divide-gray-200 dark:divide-gray-700">
            {posts |> Seq.map displayPost}
          </ul>
        </div>
      </div>
      """

  let displayPostsInCompactMode posts =
    let displayPost post =
      html
        $"""
        <tr>
          <th><a class="link link-hover" href="{post.Link}" target="_blank">{post.Title}</a></th>
          <td>{post.Source}</td>
          <td>{post.PublishedAt} ago</td>
          <td>{post.UpdatedAt} ago</td>
        </tr>
        """

    html
      $"""
      <div class="overflow-x-auto">
        <table class="table table-sm">
          <thead>
            <tr>
              <th>Title</th>
              <th>Source</th>
              <th>Published At</th>
              <th>Updated At</th>
            </tr>
          </thead>
          <tbody>
            {posts |> Seq.map displayPost}
          </tbody>
        </table>
      </div>
      """

  let displayEmptyFeed () =
    html $"<h1>Sorry, there's nothing in your feed :(</h1>"

  let nextModeIcon =
    match store.CurrentFeedMode with
    | ApplicationContext.FeedMode.Regular -> "fa-solid fa-table"
    | ApplicationContext.FeedMode.Compact -> "fa-solid fa-up-down"

  html
    $"""
    <button class="btn btn-ghost" @click={Ev(fun _ -> ApplicationContext.dispatch ApplicationContext.ToggleFeedMode)}>
      <i class="{nextModeIcon}"></i>
    </button>
    <div class="w-full h-full flex flex-col gap-y-3 justify-center items-center">
      <div class="flex flex-col sm:flex-row justify-center items-center w-full gap-x-3">
        <input
          .value={store.GetFeedRequest.SearchQuery |> Option.defaultValue String.Empty}
          @keyup={EvVal(SetSearchQuery >> dispatch)}
          type="search"
          id="search"
          class="input input-bordered"
          placeholder="Search" />
        <select class="select" @change={EvVal(ApplicationContext.SetOrdering >> ApplicationContext.dispatch)}>
          <option ?selected={store.GetFeedRequest.Ordering = Ordering.Newest}>{string Ordering.Newest}</option>
          <option ?selected={store.GetFeedRequest.Ordering = Ordering.Updated}>{string Ordering.Updated}</option>
          <option ?selected={store.GetFeedRequest.Ordering = Ordering.Oldest}>{string Ordering.Oldest}</option>
        </select>
        {FeedSelector store.GetFeedRequest.Feed store.SubscribedFeeds ApplicationContext.dispatch}
      </div>
      {if List.length store.Posts.Items > 0 then
         match store.CurrentFeedMode with
         | ApplicationContext.FeedMode.Regular -> displayPostsInRegularMode store.Posts.Items
         | ApplicationContext.FeedMode.Compact -> displayPostsInCompactMode store.Posts.Items
       else
         displayEmptyFeed ()}
      {Pagination
         store.Posts.CurrentPage
         store.Posts.PageCount
         store.Posts.HasPreviousPage
         store.Posts.HasNextPage
         (fun _ -> ApplicationContext.dispatch (ApplicationContext.SetPage(store.Posts.CurrentPage - 1)))
         (fun _ -> ApplicationContext.dispatch (ApplicationContext.SetPage(store.Posts.CurrentPage + 1)))
         (ApplicationContext.SetPage >> ApplicationContext.dispatch)}
    </div>
    """
