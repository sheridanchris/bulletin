module PostsPage

open System
open Elmish
open Fable.Core
open Feliz
open Shared

type State = {
  Model: GetPostsModel
  Posts: Paginated<PostModel>
}

type ExternalMsg =
  | NavigateToLogin
  | NavigateToRegister

type Msg =
  | Search
  | SetPage of int
  | SetSearchQuery of string
  | SetOrdering of string
  | GetPostsFromServer of Paginated<PostModel>
  | ExternalMsg of ExternalMsg
  | ToggleUpvote of Guid
  | ToggleDownvote of Guid
  | ToggleUpvoteResult of Result<VoteResult, VoteError>
  | ToggleDownvoteResult of Result<VoteResult, VoteError>

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

let handleVoteResult (state: State) (voteResult: VoteResult) =
  let postModel =
    match voteResult with
    | VoteResult.Positive model -> model
    | VoteResult.Negative model -> model
    | VoteResult.NoVote model -> model

  // let id, upvoted, downvoted =
  //   match voteResult with
  //   | VoteResult.Positive id -> id, true, false,
  //   | VoteResult.Negative id -> id, false, true
  //   | VoteResult.NoVote id -> id, false, false

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
      state, Cmd.none
    else
      let model = { state.Model with Page = page }
      { state with Model = model }, getPostsFromServerCmd model
  | SetSearchQuery query ->
    let model = { state.Model with SearchQuery = query }
    { state with Model = model }, Cmd.none
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
  | GetPostsFromServer posts -> { state with Posts = posts }, Cmd.none
  | ToggleUpvote postId -> state, Cmd.OfAsync.perform Remoting.serverApi.ToggleUpvote postId ToggleUpvoteResult
  | ToggleDownvote postId -> state, Cmd.OfAsync.perform Remoting.serverApi.ToggleDownvote postId ToggleDownvoteResult
  | ToggleUpvoteResult(Ok result)
  | ToggleDownvoteResult(Ok result) -> handleVoteResult state result, Cmd.none
  | ToggleUpvoteResult(Error _)
  | ToggleDownvoteResult(Error _) -> state, Cmd.none
  | ExternalMsg _ -> state, Cmd.none // this only exists to be relayed to the App component

[<JSX.Component>]
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

  JSX.jsx
    $"""
    <div className="border border-black">
      <div className="flex flex-row gap-x-2">
        <div className="flex flex-col justify-items-center">
          <button className={upvoteClasses} onClick={fun _ -> upvote post.Id}>▲</button>
          <button className={downvoteClasses} onClick={fun _ -> downvote post.Id}>▼</button>
        </div>
        <div>
          <div>
            <a href={post.Link} target="_blank">
              <h1 className="text-lg hover:underline hover:decoration-solid">
                {post.Title}
              </h1>
            </a>
          </div>
          <div>
            <p className="text-sm">
              {post.Score} votes • Posted by {author} {post.PublishedAt} ago • Source: {post.Source}
            </p>
          </div>
        </div>
      </div>
    </div>
    """

[<JSX.Component>]
let Pagination
  (currentPage: int)
  (numberOfPages: int)
  (hasPrevious: bool)
  (hasNext: bool)
  (goToPrevious: unit -> unit)
  (goToNext: unit -> unit)
  (goToPage: int -> unit)
  =
  JSX.jsx
    $"""
    <div className="flex flex-row gap-x-3 justify-center">
      {if hasPrevious then
         Html.button [
           prop.text "Previous"
           prop.onClick (fun _ -> goToPrevious ())
         ]
       else
         Html.none}

      {[
         for i in 1..numberOfPages do
           let isSelected = currentPage = i

           let classes =
             if isSelected then
               "text-green-700 underline decoration-solid"
             else
               ""

           Html.button [
             prop.text (string i)
             prop.onClick (fun _ -> goToPage i)
             prop.className $"hover:underline hover:decoration-solid {classes}"
           ]
       ]}

      {if hasNext then
         Html.button [
           prop.text "Next"
           prop.onClick (fun _ -> goToNext ())
         ]
       else
         Html.none}
    </div>
    """

[<JSX.Component>]
let Posts currentUser state dispatch =
  JSX.jsx
    $"""
    <div>
      <div className="flex flex-row w-full">
        <div className="mr-auto">
        {Html.input [
           prop.placeholder "search query"
           prop.onChange (SetSearchQuery >> dispatch)
         ]}
          <button onClick={fun _ -> dispatch Search}>Search</button>
          {Html.select [
             prop.className "ml-3"
             prop.onChange (SetOrdering >> dispatch)
             prop.children [
               Html.option [
                 prop.text "Top"
                 prop.selected (state.Model.Ordering = Ordering.Top)
               ]
               Html.option [
                 prop.text "Latest"
                 prop.selected (state.Model.Ordering = Ordering.Latest)
               ]
               Html.option [
                 prop.text "Oldest"
                 prop.selected (state.Model.Ordering = Ordering.Oldest)
               ]
             ]
           ]}
        </div>
        <div className="flex mr-2 ml-auto gap-x-3">
        {match currentUser with
         | Anonymous -> [
             Html.button [
               prop.text "Login"
               prop.onClick (fun _ -> dispatch (ExternalMsg NavigateToLogin))
             ]
             Html.button [
               prop.text "Register"
               prop.onClick (fun _ -> dispatch (ExternalMsg NavigateToRegister))
             ]
           ]
         | User user -> [ Html.p $"Welcome {user.Username}" ]}
        </div>
      </div>
      <div className="flex flex-col items-stretch">
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
