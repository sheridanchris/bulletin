module FeedPage

open System
open Lit
open Lit.Elmish
open LitStore
open Shared

[<HookComponent>]
let Post (post: PostModel) =
  html
    $"""
    <li class="py-3 sm:py-4">
      <div class="flex items-center space-x-4">
        <div class="flex-1 min-w-0">
          <p class="text-x-sm font-medium text-gray-900 dark:text-white">
              <a href={post.Link} target="_blank">{post.Title}</a>
          </p>
          <p class="text-sm text-gray-500 dark:text-gray-400">
              Published {post.PublishedAt} ago · Last Updated {post.UpdatedAt} ago
          </p>
        </div>
        <div class="inline-flex items-center text-base font-semibold text-gray-900 dark:text-white">
          {post.Source}
        </div>
      </div>
    </li>
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
  let pageButton (page: int) (isSelected: bool) =
    let textClasses =
      if isSelected then
        "text-blue-600 bg-blue-50"
      else
        "text-gray-500 bg-white"

    let aria = if isSelected then $""" aria-current="page" """ else ""

    html
      $"""
      <li>
        <button
          {aria}
          @click={Ev(fun _ -> goToPage page)}
          class="px-3 py-2 ml-0 leading-tight {textClasses} border
          border-gray-300 rounded-l-lg hover:bg-gray-100 hover:text-gray-700
          dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700
          dark:hover:text-white">{page}</button>
      </li>
      """

  let previousButton () =
    html
      $"""
      <button @click={Ev(fun _ -> goToPrevious ())} class="px-3 py-2 ml-0 leading-tight text-gray-500 bg-white border border-gray-300 rounded-l-lg hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white">Previous</button>
      """

  let nextButton () =
    html
      $"""
      <button @click={Ev(fun _ -> goToNext ())} class="px-3 py-2 ml-0 leading-tight text-gray-500 bg-white border border-gray-300 rounded-l-lg hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white">Next</button>
      """

  html
    $"""
    <nav class="mt-3" aria-label="Page navigation">
      <ul class="inline-flex -space-x-px">
        {[
           if hasPrevious then
             previousButton ()

           for i in 1..numberOfPages do
             let selected = currentPage = i
             pageButton i selected

           if hasNext then
             nextButton ()
         ]}
      </ul>
    </nav>
    """

[<HookComponent>]
let FeedSelector (selectedFeed: Guid option) (feeds: SubscribedFeed list) (dispatch: ApplicationContext.Msg -> unit) =
  let option (feed: SubscribedFeed) =
    html
      $"""
      <option value={string feed.FeedId} ?selected={Some feed.FeedId = selectedFeed}>{feed.Name}</option>
      """

  html
    $"""
    <select @change={EvVal(ApplicationContext.SetSelectedFeed >> dispatch)} id="feed" class="w-full sm:w-1/12 block px-4 py-3 text-base text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500">
      <option ?selected={selectedFeed = None} value="none">All Feeds</option>
      {feeds |> List.map option}
    </select>
    """

[<HookComponent>]
let Component () =
  let store = Hook.useStore ApplicationContext.store

  let displayPosts posts =
    html
      $"""
      <div class="p-4 w-full sm:w-3/4 bg-white border rounded-lg shadow-md sm:p-8 dark:bg-gray-800 dark:border-gray-700">
      <div class="flow-root">
        <ul role="list" class="divide-y divide-gray-200 dark:divide-gray-700">
          {posts |> Seq.map Post}
        </ul>
      </div>
      """

  let displayEmptyFeed () =
    html $"<h1>Sorry, there's nothing in your feed :(</h1>"

  html
    $"""
    <div class="w-full flex flex-col gap-y-3 justify-center items-center pt-20">
      <div class="flex flex-col sm:flex-row justify-center items-center w-full gap-x-3">
        <div class="w-full sm:w-3/12">
            <label for="search" class="mb-2 text-sm font-medium text-gray-900 sr-only dark:text-white">Search</label>
            <div class="relative">
                <div class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
                    <svg aria-hidden="true" class="w-5 h-5 text-gray-500 dark:text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
                </div>
                <input .value={store.GetFeedRequest.SearchQuery |> Option.defaultValue String.Empty} @change={EvVal(ApplicationContext.SetSearchQuery >> ApplicationContext.dispatch)} type="search" id="search" class="block w-full p-4 pl-10 text-sm text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" placeholder="Search">
                <button @click={Ev(fun _ -> ApplicationContext.dispatch ApplicationContext.Search)} type="submit" class="text-white absolute right-2.5 bottom-2.5 bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-4 py-2 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Search</button>
            </div>
        </div>
        <select @change={EvVal(ApplicationContext.SetOrdering >> ApplicationContext.dispatch)} id="ordering" class="w-full sm:w-1/12 block px-4 py-3 text-base text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500">
          <option ?selected={store.GetFeedRequest.Ordering = Ordering.Newest}>{string Ordering.Newest}</option>
          <option ?selected={store.GetFeedRequest.Ordering = Ordering.Updated}>{string Ordering.Updated}</option>
          <option ?selected={store.GetFeedRequest.Ordering = Ordering.Oldest}>{string Ordering.Oldest}</option>
        </select>
        {FeedSelector store.GetFeedRequest.Feed store.SubscribedFeeds ApplicationContext.dispatch}
      </div>
      {if List.length store.Posts.Items > 0 then
         displayPosts store.Posts.Items
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
