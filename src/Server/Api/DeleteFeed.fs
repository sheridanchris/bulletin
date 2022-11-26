module DeleteFeed

open FsToolkit.ErrorHandling
open FSharp.UMX
open Shared
open DependencyTypes

type DeleteFeedService = DeleteFeedRequest -> Async<Result<DeleteFeedResponse, DeleteFeedError>>

let deleteFeedService
  (getCurrentUserId: GetCurrentUserId)
  (getFeedSubscription: GetFeedSubscription)
  (deleteFeedSubscription: DeleteFeedSubscription)
  : DeleteFeedService =
  fun deleteFeedRequest -> asyncResult {
    let currentUserId = getCurrentUserId () |> Option.get

    let! feedSubscription =
      getFeedSubscription currentUserId (%deleteFeedRequest.FeedId)
      |> AsyncResult.requireSome NotFound

    do! deleteFeedSubscription feedSubscription.Id
    return Deleted(%feedSubscription.Id)
  }
