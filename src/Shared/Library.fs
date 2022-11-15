namespace Shared

open System

type UserModel = { Username: string }

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

type PostModel = {
  Id: Guid
  Title: string
  Link: string
  PublishedAt: string
  Author: string option
  Score: int
  Upvoted: bool
  Downvoted: bool
}

type Paginated<'a> = {
  Items: 'a seq
  CurrentPage: int
  PageSize: int
  PageCount: int
  HasNextPage: bool
  HasPreviousPage: bool
}

type ServerApi = {
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
