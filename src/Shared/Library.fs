namespace Shared

open System

// TODO: Model validation.

type UserModel = {
  Id: Guid
  Username: string
  EmailAddress: string
}

type CurrentUser =
  | Anonymous
  | User of UserModel

type Ordering =
  | Top
  | Latest
  | Oldest

type GetPostsModel = {
  Ordering: Ordering
  SearchQuery: string
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

type PostModel = {
  Id: Guid
  Title: string
  Link: string
  PublishedAt: string
  Author: string option
  Score: int
  Upvoted: bool
  Downvoted: bool
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

type LoginError = | InvalidUsernameAndOrPassword

type CreateAccountError =
  | UsernameTaken
  | EmailAddressTaken

type VoteResult =
  | Positive of Guid
  | Negative of Guid
  | NoVote of Guid

type VoteError =
  | Unauthorized

type ServerApi = {
  Login: LoginRequest -> Async<Result<UserModel, LoginError>>
  CreateAccount: CreateAccountRequest -> Async<Result<UserModel, CreateAccountError>>
  GetCurrentUser: unit -> Async<CurrentUser>
  ToggleUpvote: Guid -> Async<Result<VoteResult, VoteError>>
  ToggleDownvote: Guid -> Async<Result<VoteResult, VoteError>>
  GetPosts: GetPostsModel -> Async<Paginated<PostModel>>
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
