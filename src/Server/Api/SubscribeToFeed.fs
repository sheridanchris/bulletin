module SubscribeToFeed

open System
open FsToolkit.ErrorHandling
open FSharp.UMX
open Data
open Shared
open DependencyTypes

type SubscribeToFeedService = SubscribeToFeedRequest -> Async<Result<SubscribedFeed, SubscribeToFeedError>>

let private createSubscription (currentUserId: Guid<UserId>) (rssFeed: RssFeed) (feedName: string) = {
  Id = % Guid.NewGuid()
  UserId = currentUserId
  FeedId = rssFeed.Id
  FeedName = feedName
}

let private createNewFeedAndSubscription
  (saveRssFeed: SaveRssFeed)
  (saveFeedSubscription: SaveFeedSubscription)
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

    do! saveRssFeed rssFeed
    do! saveFeedSubscription rssFeedSubscription

    return
      Ok
        {
          Name = rssFeedSubscription.FeedName
          FeedUrl = rssFeed.RssFeedUrl
          FeedId = %rssFeed.Id
        }
  }

let private createNewSubscriptionForFeed
  (getFeedSubscription: GetFeedSubscription)
  (saveFeedSubscription: SaveFeedSubscription)
  (currentUserId: Guid<UserId>)
  (feed: RssFeed)
  (feedName: string)
  =
  async {
    let! feedSubscription = getFeedSubscription currentUserId feed.Id

    match feedSubscription with
    | Some _ -> return Error AlreadySubscribed
    | None ->
      let subscription = createSubscription currentUserId feed feedName
      do! saveFeedSubscription subscription

      return
        Ok
          {
            Name = subscription.FeedName
            FeedUrl = feed.RssFeedUrl
            FeedId = %subscription.Id
          }
  }

let subscribeToFeedService
  (getCurrentUserId: GetCurrentUserId)
  (getFeedByUrl: GetRssFeedByUrl)
  (getFeedSubscription: GetFeedSubscription)
  (saveRssFeed: SaveRssFeed)
  (saveFeedSubscription: SaveFeedSubscription)
  : SubscribeToFeedService =
  fun request -> async {
    let currentUserId = getCurrentUserId () |> Option.get
    let! rssFeed = getFeedByUrl request.FeedUrl

    match rssFeed with
    | Some rssFeed ->
      return!
        createNewSubscriptionForFeed getFeedSubscription saveFeedSubscription currentUserId rssFeed request.FeedName
    | None ->
      return!
        createNewFeedAndSubscription saveRssFeed saveFeedSubscription currentUserId request.FeedUrl request.FeedName
  }
