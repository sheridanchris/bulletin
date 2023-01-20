module GetSubscriptions

open Data
open FsToolkit.ErrorHandling
open Shared
open Authentication
open DataAccess
open FSharp.UMX

let private getSubscribedFeed (subscription: FeedSubscription, rssFeed: RssFeed) = {
  Id = %subscription.Id
  Name = subscription.FeedName
  FeedUrl = rssFeed.RssFeedUrl
  FeedId = %subscription.FeedId
}

let getSubscribedFeedsService
  (getCurrentUserById: GetCurrentUserId)
  (getSubscribedFeeds: GetUserSubscriptionsWithFeedsAsync)
  : GetSubscribedFeedsService =
  fun () -> async {
    let currentUserId = getCurrentUserById () |> Option.get
    return! getSubscribedFeeds currentUserId |> Async.map (List.map getSubscribedFeed)
  }
