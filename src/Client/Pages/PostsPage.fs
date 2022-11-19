module PostsPage

open System
open Fable.Core
open Lit
open LitStore
open LitRouter
open Lit.Elmish
open Shared

type State = {
  Model: GetPostsModel
  Posts: Paginated<PostModel>
}

type Msg =
  | Search
  | SetPage of int
  | SetSearchQuery of string
  | SetOrdering of string
  | GetPostsFromServer of Paginated<PostModel>
  | NavigateToLogin
  | NavigateToRegister
  | ToggleUpvote of Guid
  | ToggleDownvote of Guid
  | ToggleUpvoteResult of Result<VoteResult, VoteError>
  | ToggleDownvoteResult of Result<VoteResult, VoteError>

let getPostsFromServerCmd model =
  Elmish.Cmd.OfAsync.perform Remoting.serverApi.GetPosts model GetPostsFromServer

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

let handleVoteResult (state: State) (voteResult: VoteResult) =
  let postModel =
    match voteResult with
    | VoteResult.Positive model -> model
    | VoteResult.Negative model -> model
    | VoteResult.NoVote model -> model

  let posts =
    state.Posts.Items
    |> List.updateIf (fun post -> post.Id = postModel.Id) (fun _ -> postModel)

  let newPosts = { state.Posts with Items = posts }
  { state with Posts = newPosts }

let update (msg: Msg) (state: State) =
  match msg with
  | Search -> state, getPostsFromServerCmd state.Model
  | SetPage page ->
    if page < 0 || page > state.Posts.PageCount then
      state, Elmish.Cmd.none
    else
      let model = { state.Model with Page = page }
      { state with Model = model }, getPostsFromServerCmd model
  | SetSearchQuery query ->
    let model = { state.Model with SearchQuery = query }
    { state with Model = model }, Elmish.Cmd.none
  | SetOrdering ordering ->
    let ordering =
      match ordering with
      | "Top" -> Ordering.Top
      | "Oldest" -> Ordering.Oldest
      | "Latest"
      | _ -> Ordering.Latest

    let model =
      { state.Model with
          Ordering = ordering
          Page = 1
      }

    { state with Model = model }, getPostsFromServerCmd model
  | NavigateToLogin -> state, Cmd.navigate "login"
  | NavigateToRegister -> state, Cmd.navigate "register"
  | GetPostsFromServer posts -> { state with Posts = posts }, Elmish.Cmd.none
  | ToggleUpvote postId -> state, Elmish.Cmd.OfAsync.perform Remoting.serverApi.ToggleUpvote postId ToggleUpvoteResult
  | ToggleDownvote postId ->
    state, Elmish.Cmd.OfAsync.perform Remoting.serverApi.ToggleDownvote postId ToggleDownvoteResult
  | ToggleUpvoteResult(Ok result)
  | ToggleDownvoteResult(Ok result) -> handleVoteResult state result, Elmish.Cmd.none
  | ToggleUpvoteResult(Error _)
  | ToggleDownvoteResult(Error _) -> state, Elmish.Cmd.none

[<HookComponent>]
let Post (upvote: Guid -> unit) (downvote: Guid -> unit) (post: PostModel) =
  let author = post.Author |> Option.defaultValue "automated bot"

  let upvoteClasses =
    if post.Upvoted then
      "hover:text-black text-lime-400"
    else
      "hover:text-lime-400 text-black"

  let downvoteClasses =
    if post.Downvoted then
      "hover:text-black text-red-500"
    else
      "hover:text-red-500 text-black"

  html
    $"""
    <div class="border border-black">
      <div class="flex flex-row gap-x-2">
        <div class="flex flex-col justify-items-center">
          <button class={upvoteClasses} @click={Ev(fun _ -> upvote post.Id)}>▲</button>
          <button class={downvoteClasses} @click={Ev(fun _ -> downvote post.Id)}>▼</button>
        </div>
        <div>
          <div>
            <a href={post.Link} target="_blank">
              <h1 class="text-lg hover:underline hover:decoration-solid">
                {post.Title}
              </h1>
            </a>
          </div>
          <div>
            <p class="text-sm">
              {post.Score} votes • Posted by {author} {post.PublishedAt} ago • Source: {post.Source}
            </p>
          </div>
        </div>
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
  let store = Hook.useStore (ApplicationContext.store)

  let renderAnonymous =
    html
      $"""
      <button @click={Ev(fun _ -> dispatch NavigateToLogin)}>Login</button>
      <button @click={Ev(fun _ -> dispatch NavigateToRegister)}>Register</button>
      """

  let renderUser (user: UserModel) =
    html
      $"""
      <p>Welcome {user.Username}</p>
      """

  html
    $"""
    <div>
      <div class="flex flex-row w-full">
        <div class="mr-auto">
          <input placeholder="search query" @change="{EvVal(SetSearchQuery >> dispatch)}" />
          <button @click={Ev(fun _ -> dispatch Search)}>Search</button>
          <select class="ml-3" @change={EvVal(SetOrdering >> dispatch)}>
            <option @selected={state.Model.Ordering = Ordering.Top}>Top</option>
            <option @selected={state.Model.Ordering = Ordering.Latest}>Latest</option>
            <option @selected={state.Model.Ordering = Ordering.Oldest}>Oldest</option>
          </select>
        </div>
        <div class="flex mr-2 ml-auto gap-x-3">
        {match store.User with
         | Anonymous -> renderAnonymous
         | User user -> renderUser user}
        </div>
      </div>
      <div class="flex flex-col items-stretch">
        {let post = Post (ToggleUpvote >> dispatch) (ToggleDownvote >> dispatch)
         state.Posts.Items |> Seq.map post}
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
