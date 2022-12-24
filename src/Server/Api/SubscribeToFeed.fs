module SubscribeToFeed

open System
open FsToolkit.ErrorHandling
open FSharp.UMX
open Data
open DataAccess
open Shared
open DependencyTypes

let private createSubscription (currentUserId: Guid<UserId>) (rssFeed: RssFeed) (feedName: string) = {
  Id = % Guid.NewGuid()
  UserId = currentUserId
  FeedId = rssFeed.Id
  FeedName = feedName
}

let private createNewFeedAndSubscription
  (saveRssFeedAsync: SaveAsync<RssFeed>)
  (saveFeedSubscriptionAsync: SaveAsync<FeedSubscription>)
  (currentUserId: Guid<UserId>)
  (feedUrl: string)
  (feedName: string)
  =
  async {
    let rssFeed = {
      Id = % Guid.NewGuid()
      RssFeedUrl = feedUrl
    }

    let rssFeedSubscription = createSubscription currentUserId rssFeed feedName

    do! saveRssFeedAsync rssFeed
    do! saveFeedSubscriptionAsync rssFeedSubscription

    return
      Ok
        {
          Id = %rssFeedSubscription.Id
          Name = rssFeedSubscription.FeedName
          FeedUrl = rssFeed.RssFeedUrl
          FeedId = %rssFeed.Id
        }
  }

let private createNewSubscriptionForFeed
  (getFeedSubscriptionAsync: GetUserFeedSubscriptionAsync)
  (saveFeedSubscriptionAsync: SaveAsync<FeedSubscription>)
  (currentUserId: Guid<UserId>)
  (feed: RssFeed)
  (feedName: string)
  =
  asyncResult {
    do!
      getFeedSubscriptionAsync currentUserId feed.Id
      |> AsyncResult.requireNone AlreadySubscribed

    let subscription = createSubscription currentUserId feed feedName
    do! saveFeedSubscriptionAsync subscription

    return {
      Id = %subscription.Id
      Name = subscription.FeedName
      FeedUrl = feed.RssFeedUrl
      FeedId = %subscription.FeedId
    }
  }

let subscribeToFeedService
  (getCurrentUserId: GetCurrentUserId)
  (getFeedByUrl: GetRssFeedByUrl)
  (getFeedSubscriptionAsync: GetUserFeedSubscriptionAsync)
  (saveRssFeed: SaveAsync<RssFeed>)
  (saveFeedSubscription: SaveAsync<FeedSubscription>)
  : SubscribeToFeedService =
  fun request -> async {
    let currentUserId = getCurrentUserId () |> Option.get
    let! rssFeed = getFeedByUrl request.FeedUrl

    match rssFeed with
    | Some rssFeed ->
      return!
        createNewSubscriptionForFeed getFeedSubscriptionAsync saveFeedSubscription currentUserId rssFeed request.FeedName
    | None ->
      return!
        createNewFeedAndSubscription saveRssFeed saveFeedSubscription currentUserId request.FeedUrl request.FeedName
  }
