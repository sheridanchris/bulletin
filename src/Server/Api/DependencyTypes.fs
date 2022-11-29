module DependencyTypes

open FSharp.UMX
open Data
open Marten.Pagination
open Shared

type FindUserById = Guid<UserId> -> Async<User option>
type FindUserByName = string -> Async<User option>
type FindUserByEmailAddress = string -> Async<User option>
type CreatePasswordHash = string -> string
type VerifyPasswordHash = string -> User -> bool
type SaveUser = User -> Async<unit>
type SignInUser = User -> Async<unit>
type GetCurrentUserId = unit -> Guid<UserId> option
type GetRssFeedByUrl = string -> Async<RssFeed option>
type SaveRssFeed = RssFeed -> Async<unit>
type SaveFeedSubscription = FeedSubscription -> Async<unit>
type GetFeedSubscription = Guid<UserId> -> Guid<FeedId> -> Async<FeedSubscription option>
type DeleteFeedSubscription = Guid<FeedSubscriptionId> -> Async<unit>
type GetSubscribedFeeds = Guid<UserId> -> Async<(FeedSubscription * RssFeed) list>
type GetUserFeed = GetFeedRequest -> Guid<FeedId> array -> Async<IPagedList<Post>>
type CreateGravatarUrl = string -> string
