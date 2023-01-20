module GetUserFeed

open System
open Data
open FSharp.UMX
open FsToolkit.ErrorHandling
open Authentication
open Marten.Pagination
open Shared
open DataAccess

let private toPaginated (mapping: 'a -> 'b) (pagedList: IPagedList<'a>) : Paginated<'b> = {
  Items = pagedList |> Seq.map mapping |> Seq.toList
  CurrentPage = int pagedList.PageNumber
  PageSize = int pagedList.PageSize
  PageCount = int pagedList.PageCount
  HasNextPage = pagedList.HasNextPage
  HasPreviousPage = pagedList.HasPreviousPage
}

let private getPostModel (subscribedFeeds: FeedSubscription list) (post: Post) : PostModel =
  let now = DateTime.UtcNow
  let publishedAt = DateTime.friendlyDifference post.PublishedAt now
  let updatedAt = DateTime.friendlyDifference post.LastUpdatedAt now

  let source =
    subscribedFeeds
    |> List.tryFind (fun feed -> feed.FeedId = %post.Feed)
    |> Option.map (fun subscribedFeed -> subscribedFeed.FeedName)
    |> Option.defaultValue "Unknown Feed"

  {
    Id = %post.Id
    Title = post.Headline
    Link = post.Link
    PublishedAt = publishedAt
    UpdatedAt = updatedAt
    Source = source
  }

let getUserFeedService
  (getCurrentUserId: GetCurrentUserId)
  (getSubscribedFeeds: GetUserSubscriptionsWithFeedsAsync)
  (getFeed: GetUserFeedAsync)
  : GetUserFeedService =
  fun request -> async {
    let currentUserId = getCurrentUserId () |> Option.get
    let! subscribedFeeds = getSubscribedFeeds currentUserId |> Async.map (List.map fst)

    return!
      getFeed request [| for feed in subscribedFeeds -> feed.FeedId |]
      |> Async.map (toPaginated (getPostModel subscribedFeeds))
  }
