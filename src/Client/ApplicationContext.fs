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

let private getUserFeedCmd model =
  Cmd.OfAsync.perform Remoting.securedServerApi.GetUserFeed model.GetFeedRequest SetPosts

let private setUser user model =
  match user with
  | User user ->
    let model = { model with User = User user }

    model,
    Cmd.batch [
      Cmd.OfAsync.perform Remoting.securedServerApi.GetSubscribedFeeds () SetSubscribedFeeds
      getUserFeedCmd model
    ]
  | Anonymous ->
    { model with
        User = Anonymous
        SubscribedFeeds = []
        Posts = Paginated.empty ()
    },
    Cmd.none

let private setOrdering ordering model =
  let ordering =
    match ordering with
    | ordering when ordering = string Oldest -> Oldest
    | ordering when ordering = string Newest -> Newest
    | ordering when ordering = string Updated -> Updated
    | _ -> Updated

  let getFeedRequest =
    { model.GetFeedRequest with
        Ordering = ordering
        Page = 1
    }

  let model =
    { model with
        GetFeedRequest = getFeedRequest
    }

  model, getUserFeedCmd model

let deleteFeed feedId model =
  let subscribedFeed =
    model.SubscribedFeeds
    |> List.indexed
    |> List.tryFind (fun (_, subscription) -> subscription.Id = feedId)

  match subscribedFeed with
  | None -> model, Cmd.none
  | Some(index, _) ->
    // TODO: not sure if I should update the feed here, or elsewhere.
    let subscribedFeeds = List.removeAt index model.SubscribedFeeds

    let model =
      { model with
          SubscribedFeeds = subscribedFeeds
      }

    model, getUserFeedCmd model

let private search model =
  let getFeedRequest = { model.GetFeedRequest with Page = 1 }

  let model =
    { model with
        GetFeedRequest = getFeedRequest
    }

  model, getUserFeedCmd model

let private setSearchQuery query model =
  let query = if String.IsNullOrWhiteSpace query then None else Some query

  let getFeedRequest =
    { model.GetFeedRequest with
        SearchQuery = query
    }

  let model =
    { model with
        GetFeedRequest = getFeedRequest
    }

  model, getUserFeedCmd model

let private setPage page model =
  match page with
  | page when page < 0 || page > model.Posts.PageCount -> model, Cmd.none
  | page ->
    let getFeedRequest =
      { model.GetFeedRequest with
          Page = page
      }

    let model =
      { model with
          GetFeedRequest = getFeedRequest
      }

    model, getUserFeedCmd model

let private setSelectedFeed (feedValueString: string) model =
  let feedId =
    match Guid.TryParse(feedValueString) with
    | true, value -> Some value
    | false, _ -> None

  let getFeedRequest =
    { model.GetFeedRequest with
        Feed = feedId
        Page = 1
    }

  let model =
    { model with
        GetFeedRequest = getFeedRequest
    }

  model, getUserFeedCmd model

let update (msg: Msg) (model: Model) =
  match msg with
  | SetPosts posts -> { model with Posts = posts }, Cmd.none
  | SetSubscribedFeeds feeds -> { model with SubscribedFeeds = feeds }, Cmd.none
  | AddFeedToContext feed ->
    { model with
        SubscribedFeeds = feed :: model.SubscribedFeeds
    },
    getUserFeedCmd model
  | SetCurrentUser user -> setUser user model
  | DeleteFeedFromContext feedId -> deleteFeed feedId model
  | Search -> search model
  | SetPage page -> setPage page model
  | SetSearchQuery query -> setSearchQuery query model
  | SetOrdering ordering -> setOrdering ordering model
  | SetSelectedFeed feedValue -> setSelectedFeed feedValue model

let dispose _ = ()
let store, dispatch = Store.makeElmish init update dispose ()
