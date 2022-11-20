module GetSubscriptions

open Data
open FsToolkit.ErrorHandling
open Shared
open DependencyTypes
open FSharp.UMX

type SubscribedFeedsService = unit -> Async<SubscribedFeed list>

let private getSubscribedFeed (subscription: FeedSubscription, rssFeed: RssFeed) = {
  Name = subscription.FeedName
  FeedUrl = rssFeed.RssFeedUrl
  FeedId = %subscription.FeedId
}

let getSubscribedFeedsService
  (getCurrentUserById: GetCurrentUserId)
  (getSubscribedFeeds: GetSubscribedFeeds)
  : SubscribedFeedsService =
  fun () -> async {
    let currentUserId = getCurrentUserById () |> Option.get
    return! getSubscribedFeeds currentUserId |> Async.map (List.map getSubscribedFeed)
  }
