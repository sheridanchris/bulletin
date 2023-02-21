module DataAccess

open System
open System.Collections.Generic
open Marten
open Marten.Pagination
open FsToolkit.ErrorHandling
open Data
open System.Linq
open Shared

type GetRssFeeds = unit -> Async<IReadOnlyList<RssFeed>>

let getRssFeeds (querySession: IQuerySession) : GetRssFeeds =
  fun () -> querySession |> Session.query<RssFeed> |> Queryable.toListAsync

type GetRssFeedByUrl = string -> Async<RssFeed option>

let getRssFeedByUrlAsync (querySession: IQuerySession) : GetRssFeedByUrl =
  fun feedUrl ->
    querySession
    |> Session.query<RssFeed>
    |> Queryable.filter <@ fun feed -> feed.RssFeedUrl = feedUrl @>
    |> Queryable.tryHeadAsync

type GetLatestPostAsync = unit -> Async<DateTime option>

let latestPostAsync (querySession: IQuerySession) : GetLatestPostAsync =
  fun () ->
    querySession
    |> Session.query<Post>
    |> Queryable.orderByDescending <@ fun post -> post.LastUpdatedAt @>
    |> Queryable.tryHeadAsync
    |> AsyncOption.map (fun post -> post.LastUpdatedAt)

type FindPostsByUrls = string[] -> Async<IReadOnlyList<Post>>

let findPostsByUrls (querySession: IQuerySession) : FindPostsByUrls =
  fun urls ->
    querySession
    |> Session.query<Post>
    |> Queryable.filter <@ fun post -> post.Link.IsOneOf(urls) @>
    |> Queryable.toListAsync

type GetUserFeedSubscriptionAsync = UserId -> FeedId -> Async<FeedSubscription option>

let getUserFeedSubscriptionAsync (querySession: IQuerySession) : GetUserFeedSubscriptionAsync =
  fun userId feedId ->
    querySession
    |> Session.query<FeedSubscription>
    |> Queryable.filter <@ fun subscription -> subscription.UserId = userId && subscription.FeedId = feedId @>
    |> Queryable.tryHeadAsync

type GetUserSubscriptionsWithFeedsAsync = UserId -> Async<(FeedSubscription * RssFeed) list>

let getAllUserSubscriptionsWithFeeds (querySession: IQuerySession) : GetUserSubscriptionsWithFeedsAsync =
  fun userId ->
    let join (dict: Dictionary<FeedId, RssFeed>) (feedSubscription: FeedSubscription) =
      let correspondingRssFeed = dict[feedSubscription.FeedId]
      feedSubscription, correspondingRssFeed

    let dict: Dictionary<FeedId, RssFeed> = Dictionary()

    querySession
    |> Session.query<FeedSubscription>
    |> Queryable.filter <@ fun subscription -> subscription.UserId = userId @>
    |> Queryable.includeDict <@ fun subscription -> subscription.FeedId @> dict
    |> Queryable.toListAsync
    |> Async.map (Seq.map (join dict) >> Seq.toList)

type GetUserFeedAsync = GetFeedRequest -> FeedId array -> Async<IPagedList<Post>>

let getUserFeedAsync (querySession: IQuerySession) : GetUserFeedAsync =
  fun request feedIds ->
    let orderPosts: IQueryable<Post> -> IOrderedQueryable<Post> =
      match request.Ordering with
      | Updated -> Queryable.orderByDescending <@ fun post -> post.LastUpdatedAt @>
      | Newest -> Queryable.orderByDescending <@ fun post -> post.PublishedAt @>
      | Oldest -> Queryable.orderBy <@ fun post -> post.PublishedAt @>

    let filterPostsByFeed: IQueryable<Post> -> IQueryable<Post> =
      match request.Feed with
      | None -> id
      | Some feedId -> Queryable.filter <@ fun post -> post.Feed = feedId @>

    let filterPostsByHeadline: IQueryable<Post> -> IQueryable<Post> =
      match request.SearchQuery with
      | None -> id
      | Some query -> Queryable.filter <@ fun post -> post.Headline.PhraseSearch(query) @>

    querySession
    |> Session.query<Post>
    |> Queryable.filter <@ fun post -> post.Feed.IsOneOf(feedIds) @>
    |> filterPostsByFeed
    |> filterPostsByHeadline
    |> orderPosts
    |> Queryable.pagedListAsync request.Page request.PageSize

type GetUserFilter =
  | FindById of UserId
  | FindByUsername of string
  | FindByEmailAddress of string

type FindUserAsync = GetUserFilter -> Async<User option>

let tryFindUserAsync (querySession: IQuerySession) : FindUserAsync =
  fun filter ->
    let filter: IQueryable<User> -> IQueryable<User> =
      match filter with
      | FindById id -> Queryable.filter <@ fun user -> user.Id = id @>
      | FindByUsername username -> Queryable.filter <@ fun user -> user.Username = username @>
      | FindByEmailAddress email -> Queryable.filter <@ fun user -> user.EmailAddress = email @>

    querySession |> Session.query<User> |> filter |> Queryable.tryHeadAsync

type FindCategoryByNameForUser = UserId -> string -> Async<Category option>

let findCategoryByNameForUser (querySession: IQuerySession) : FindCategoryByNameForUser =
  fun userId categoryName ->
    querySession
    |> Session.query<Category>
    |> Queryable.filter <@ fun category -> category.Name = categoryName && category.UserId = userId @>
    |> Queryable.tryHeadAsync

type SaveAsync<'T> = 'T -> Async<unit>

let saveAsync (documentSession: IDocumentSession) : SaveAsync<'T> =
  fun elem ->
    documentSession |> Session.storeSingle elem
    documentSession |> Session.saveChangesAsync

type SaveManyAsync<'T> = 'T list -> Async<unit>

let saveManyAsync (documentSession: IDocumentSession) : SaveManyAsync<'T> =
  fun elems ->
    documentSession |> Session.storeMany elems
    documentSession |> Session.saveChangesAsync

type DeleteAsync<'T> = 'T -> Async<unit>

let deleteAsync (documentSession: IDocumentSession) : DeleteAsync<'T> =
  fun entity ->
    documentSession |> Session.deleteEntity entity
    documentSession |> Session.saveChangesAsync
