module ApplicationContext

open System
open Shared
open ElmishStore

// This acts as a cache for the entire app so I don't have to send network requests on navigation to
// profile, feed, or subscriptions...

type Model = {
  User: CurrentUser
  SubscribedFeeds: SubscribedFeed list
  Posts: Paginated<PostModel>
  GetFeedRequest: GetFeedRequest
}

type Msg =
  | SetCurrentUser of CurrentUser
  | SetSubscribedFeeds of SubscribedFeed list
  | AddFeedToContext of SubscribedFeed
  | DeleteFeedFromContext of Guid
  | Search
  | SetPage of int
  | SetSearchQuery of string
  | SetOrdering of string
  | SetSelectedFeed of string
  | SetPosts of Paginated<PostModel>

let init () =
  {
    User = Anonymous
    SubscribedFeeds = []
    Posts = Paginated.empty ()
    GetFeedRequest =
      {
        Ordering = Newest
        SearchQuery = None
        Feed = None
        Page = 1
        PageSize = 25
      }
  },
  Cmd.OfAsync.perform Remoting.unsecuredServerApi.GetCurrentUser () SetCurrentUser

let private getUserFeedFromServerCmd request =
  Cmd.OfAsync.perform Remoting.securedServerApi.GetUserFeed request SetPosts

let update (msg: Msg) (model: Model) =
  match msg with
  | SetCurrentUser user ->
    match user with
    | User user ->
      { model with User = User user },
      Cmd.batch [
        Cmd.OfAsync.perform Remoting.securedServerApi.GetSubscribedFeeds () SetSubscribedFeeds
        getUserFeedFromServerCmd model.GetFeedRequest
      ]
    | Anonymous ->
      { model with
          User = Anonymous
          SubscribedFeeds = []
          Posts = Paginated.empty ()
      },
      Cmd.none
  | SetSubscribedFeeds feeds -> { model with SubscribedFeeds = feeds }, Cmd.none
  | AddFeedToContext feed ->
    // TODO: not sure if I should update the feed here, or elsewhere.
    { model with SubscribedFeeds = feed :: model.SubscribedFeeds }, getUserFeedFromServerCmd model.GetFeedRequest
  | DeleteFeedFromContext feedId ->
    let subscribedFeed =
      model.SubscribedFeeds
      |> List.indexed
      |> List.tryFind (fun (_, subscription) -> subscription.Id = feedId)

    match subscribedFeed with
    | Some(index, _) ->
      // TODO: not sure if I should update the feed here, or elsewhere.
      { model with SubscribedFeeds = List.removeAt index model.SubscribedFeeds },
      getUserFeedFromServerCmd model.GetFeedRequest
    | None -> model, Cmd.none
  | Search ->
    let getFeedRequest = { model.GetFeedRequest with Page = 1 }
    { model with GetFeedRequest = getFeedRequest }, getUserFeedFromServerCmd model.GetFeedRequest
  | SetPage page ->
    if page < 0 || page > model.Posts.PageCount then
      model, Cmd.none
    else
      let getFeedRequest = { model.GetFeedRequest with Page = page }
      { model with GetFeedRequest = getFeedRequest }, getUserFeedFromServerCmd model.GetFeedRequest
  | SetSearchQuery query ->
    let query = if String.IsNullOrWhiteSpace query then None else Some query
    let getFeedRequest = { model.GetFeedRequest with SearchQuery = query }
    { model with GetFeedRequest = getFeedRequest }, Cmd.none
  | SetOrdering ordering ->
    let ordering =
      match ordering with
      | ordering when ordering = string Ordering.Updated -> Ordering.Updated
      | ordering when ordering = string Ordering.Oldest -> Ordering.Oldest
      | ordering when ordering = string Ordering.Newest -> Ordering.Newest
      | _ -> Ordering.Updated

    let getFeedRequest =
      { model.GetFeedRequest with
          Ordering = ordering
          Page = 1
      }

    { model with GetFeedRequest = getFeedRequest }, getUserFeedFromServerCmd model.GetFeedRequest
  | SetSelectedFeed feedValue ->
    let feedId =
      match Guid.TryParse(feedValue) with
      | true, value -> Some value
      | false, _ -> None

    let getFeedRequest =
      { model.GetFeedRequest with
          Feed = feedId
          Page = 1
      }

    { model with GetFeedRequest = getFeedRequest }, getUserFeedFromServerCmd model.GetFeedRequest
  | SetPosts posts -> { model with Posts = posts }, Cmd.none

let dispose _ = ()
let store, dispatch = Store.makeElmish init update dispose ()
