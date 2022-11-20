module DataAccess

open System
open System.Collections.Generic
open System.Threading
open Marten
open FsToolkit.ErrorHandling
open Data
open System.Linq
open Shared
open FSharp.UMX

let getRssFeeds (querySession: IQuerySession) =
  querySession |> Session.query<RssFeed> |> Queryable.toListAsync

let getRssFeedByUrlAsync (feedUrl: string) (querySession: IQuerySession) =
  querySession
  |> Session.query<RssFeed>
  |> Queryable.filter <@ fun feed -> feed.RssFeedUrl = feedUrl @>
  |> Queryable.tryHeadAsync

let latestPostAsync (querySession: IQuerySession) =
  querySession
  |> Session.query<Post>
  |> Queryable.orderByDescending <@ fun post -> post.LastUpdatedAt @>
  |> Queryable.tryHeadAsync
  |> AsyncOption.map (fun post -> post.LastUpdatedAt)

let findPostsByUrls (links: string[]) (querySession: IQuerySession) =
  querySession
  |> Session.query<Post>
  |> Queryable.filter <@ fun post -> post.Link.IsOneOf(links) @>
  |> Queryable.toListAsync

let getUserFeedSubscriptionAsync (userId: Guid<UserId>) (feedId: Guid<FeedId>) (querySession: IQuerySession) =
  querySession
  |> Session.query<FeedSubscription>
  |> Queryable.filter <@ fun subscription -> subscription.UserId = userId && subscription.FeedId = feedId @>
  |> Queryable.tryHeadAsync

let getAllUserSubscriptionsWithFeeds (userId: Guid<UserId>) (querySession: IQuerySession) =
  let join (dict: Dictionary<Guid<FeedId>, RssFeed>) (feedSubscription: FeedSubscription) =
    let correspondingRssFeed = dict[feedSubscription.FeedId]
    feedSubscription, correspondingRssFeed

  let dict: Dictionary<Guid<FeedId>, RssFeed> = Dictionary()

  querySession
  |> Session.query<FeedSubscription>
  |> Queryable.filter <@ fun subscription -> subscription.UserId = userId @>
  |> Queryable.includeDict <@ fun subscription -> subscription.FeedId @> dict
  |> Queryable.toListAsync
  |> Async.map (Seq.map (join dict) >> Seq.toList)

let getUserFeedAsync (criteria: GetFeedRequest) (subscribedFeeds: Guid<FeedId> array) (querySession: IQuerySession) =
  let orderPosts (queryable: IQueryable<Post>) =
    match criteria.Ordering with
    | Updated -> queryable |> Queryable.orderByDescending <@ fun post -> post.LastUpdatedAt @>
    | Newest -> queryable |> Queryable.orderByDescending <@ fun post -> post.PublishedAt @>
    | Oldest -> queryable |> Queryable.orderBy <@ fun post -> post.PublishedAt @>

  let filterPostsByFeed (queryable: IQueryable<Post>) =
    match criteria.Feed with
    | None -> queryable
    | Some feedId -> queryable |> Queryable.filter <@ fun post -> post.Feed = %feedId @>

  let filterPostsByHeadline (queryable: IQueryable<Post>) =
    match criteria.SearchQuery with
    | None -> queryable
    | Some query ->
      queryable
      |> Queryable.filter <@ fun post -> post.Headline.PhraseSearch(query) @>

  querySession
  |> Session.query<Post>
  |> Queryable.filter <@ fun post -> post.Feed.IsOneOf(subscribedFeeds) @>
  |> filterPostsByFeed
  |> filterPostsByHeadline
  |> orderPosts
  |> Queryable.pagedListAsync criteria.Page criteria.PageSize

let tryFindPostAsync (postId: Guid<PostId>) (querySession: IQuerySession) =
  querySession
  |> Session.query<Post>
  |> Queryable.filter <@ fun post -> post.Id = postId @>
  |> Queryable.tryHeadAsync

type GetUserFilter =
  | FindById of Guid<UserId>
  | FindByUsername of string
  | FindByEmailAddress of string

let tryFindUserAsync (filter: GetUserFilter) (querySession: IQuerySession) =
  let filter (queryable: IQueryable<User>) : IQueryable<User> =
    match filter with
    | FindById id -> queryable |> Queryable.filter <@ fun user -> user.Id = id @>
    | FindByUsername username -> queryable |> Queryable.filter <@ fun user -> user.Username = username @>
    | FindByEmailAddress email -> queryable |> Queryable.filter <@ fun user -> user.EmailAddress = email @>

  querySession |> Session.query<User> |> filter |> Queryable.tryHeadAsync

let saveUserAsync (user: User) (documentSession: IDocumentSession) =
  documentSession |> Session.storeSingle user
  documentSession |> Session.saveChangesAsync

let saveRssFeedAsync (rssFeed: RssFeed) (documentSession: IDocumentSession) =
  documentSession |> Session.storeSingle rssFeed
  documentSession |> Session.saveChangesAsync

let saveFeedSubscriptionAsync (feedSubscription: FeedSubscription) (documentSession: IDocumentSession) =
  documentSession |> Session.storeSingle feedSubscription
  documentSession |> Session.saveChangesAsync

let savePostAsync (post: Post) (documentSession: IDocumentSession) =
  documentSession |> Session.storeSingle post
  documentSession |> Session.saveChangesAsync
