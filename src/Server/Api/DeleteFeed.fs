module DeleteFeed

open FsToolkit.ErrorHandling
open FSharp.UMX
open Shared
open DependencyTypes
open DataAccess
open Data

let deleteFeedService
  (getCurrentUserId: GetCurrentUserId)
  (getFeedSubscriptionAsync: GetUserFeedSubscriptionAsync)
  (deleteAsync: DeleteAsync<FeedSubscription>)
  : DeleteFeedService =
  fun deleteFeedRequest -> asyncResult {
    let currentUserId = getCurrentUserId () |> Option.get

    let! feedSubscription =
      getFeedSubscriptionAsync currentUserId (%deleteFeedRequest.FeedId)
      |> AsyncResult.requireSome NotFound

    do! deleteAsync feedSubscription
    return Deleted(%feedSubscription.Id)
  }
