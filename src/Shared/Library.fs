namespace Shared

open System
open FSharp.UMX

// TODO: Model validation.

[<Measure>]
type feedId

[<Measure>]
type userId

[<Measure>]
type postId

[<Measure>]
type feedSubscriptionId

type FeedId = Guid<feedId>
type UserId = Guid<userId>
type PostId = Guid<postId>
type SubscriptionId = Guid<feedSubscriptionId>

type UserModel = {
  Id: UserId
  Username: string
  EmailAddress: string
  GravatarEmailAddress: string
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
  Feed: FeedId option
  Page: int
  PageSize: int
}

type LoginRequest = { Username: string; Password: string }

type CreateAccountRequest = {
  Username: string
  EmailAddress: string
  Password: string
}

type SubscribeToFeedRequest = { FeedName: string; FeedUrl: string }

type DeleteFeedRequest = { FeedId: FeedId }

type EditUserProfileRequest = {
  Username: string option
  EmailAddress: string option
  GravatarEmailAddress: string option
}

type ChangePasswordRequest = {
  CurrentPassword: string
  NewPassword: string
}

type PostModel = {
  Id: PostId
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
  Id: SubscriptionId
  Name: string
  FeedUrl: string
  FeedId: FeedId
}

type LoginError = | InvalidUsernameAndOrPassword

type CreateAccountError =
  | UsernameTaken
  | EmailAddressTaken

type SubscribeToFeedError = | AlreadySubscribed

type DeleteFeedError = | NotFound

type DeleteFeedResponse = Deleted of SubscriptionId

type EditUserProfileError = | UserNotFound

type ChangePasswordError =
  | PasswordsDontMatch
  | UserNotFound

type LoginService = LoginRequest -> Async<Result<UserModel, LoginError>>
type CreateAccountService = CreateAccountRequest -> Async<Result<UserModel, CreateAccountError>>
type GetCurrentUserService = unit -> Async<CurrentUser>

type UnsecuredServerApi = {
  Login: LoginService
  CreateAccount: CreateAccountService
  GetCurrentUser: GetCurrentUserService
}

type GetSubscribedFeedsService = unit -> Async<SubscribedFeed list>
type GetUserFeedService = GetFeedRequest -> Async<Paginated<PostModel>>
type SubscribeToFeedService = SubscribeToFeedRequest -> Async<Result<SubscribedFeed, SubscribeToFeedError>>
type DeleteFeedService = DeleteFeedRequest -> Async<Result<DeleteFeedResponse, DeleteFeedError>>
type EditUserProfileService = EditUserProfileRequest -> Async<Result<UserModel, EditUserProfileError>>
type ChangePasswordService = ChangePasswordRequest -> Async<Result<unit, ChangePasswordError>>

type SecuredServerApi = {
  GetSubscribedFeeds: GetSubscribedFeedsService
  GetUserFeed: GetUserFeedService
  SubscribeToFeed: SubscribeToFeedService
  DeleteFeed: DeleteFeedService
  EditUserProfile: EditUserProfileService
  ChangePassword: ChangePasswordService
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
