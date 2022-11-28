namespace Shared

open System
open Microsoft.FSharp.Core

// TODO: Model validation.

type UserModel = {
  Id: Guid
  Username: string
  EmailAddress: string
  ProfilePictureUrl: string
}

type CurrentUser =
  | Anonymous
  | User of UserModel

type Ordering =
  | Newest
  | Oldest
  | Updated

type GetFeedRequest = {
  Ordering: Ordering
  SearchQuery: string option
  Feed: Guid option
  Page: int
  PageSize: int
}

type LoginRequest = {
  Username: string
  Password: string
}

type CreateAccountRequest = {
  Username: string
  EmailAddress: string
  Password: string
}

type SubscribeToFeedRequest = {
  FeedName: string
  FeedUrl: string
}

type DeleteFeedRequest = { FeedId: Guid }

type PostModel = {
  Id: Guid
  Title: string
  Link: string
  PublishedAt: string
  UpdatedAt: string
  Source: string
}

type Paginated<'a> = {
  Items: 'a list
  CurrentPage: int
  PageSize: int
  PageCount: int
  HasNextPage: bool
  HasPreviousPage: bool
}

type SubscribedFeed = {
  Id: Guid
  Name: string
  FeedUrl: string
  FeedId: Guid
}

type LoginError = | InvalidUsernameAndOrPassword

type CreateAccountError =
  | UsernameTaken
  | EmailAddressTaken

type SubscribeToFeedError = | AlreadySubscribed

type DeleteFeedError = | NotFound

type DeleteFeedResponse = Deleted of Guid

type UnsecuredServerApi = {
  Login: LoginRequest -> Async<Result<UserModel, LoginError>>
  CreateAccount: CreateAccountRequest -> Async<Result<UserModel, CreateAccountError>>
  GetCurrentUser: unit -> Async<CurrentUser>
}

type SecuredServerApi = {
  GetSubscribedFeeds: unit -> Async<SubscribedFeed list>
  GetUserFeed: GetFeedRequest -> Async<Paginated<PostModel>>
  SubscribeToFeed: SubscribeToFeedRequest -> Async<Result<SubscribedFeed, SubscribeToFeedError>>
  DeleteFeed: DeleteFeedRequest -> Async<Result<DeleteFeedResponse, DeleteFeedError>>
}

module Paginated =
  let empty () = {
    Items = []
    CurrentPage = 0
    PageSize = 0
    PageCount = 0
    HasNextPage = false
    HasPreviousPage = false
  }
