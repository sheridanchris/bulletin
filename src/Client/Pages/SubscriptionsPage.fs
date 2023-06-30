module SubscriptionsPage

open System
open Components
open Components.Alerts
open Lit
open Lit.Elmish
open LitStore
open Shared
open Validus
open Browser.Dom

type State = {
  Request: SubscribeToFeedRequest
  ValidationErrors: Map<string, string list>
  Alert: Alert option
}

type Msg =
  | SetFeedName of string
  | SetFeedUrl of string
  | Subscribe
  | DeleteFeed of FeedId
  | SubscriptionResult of Result<SubscribedFeed, SubscribeToFeedError>
  | DeleteFeedResult of Result<DeleteFeedResponse, DeleteFeedError>
  | GotException of exn

let init () =
  {
    Request = { FeedName = ""; FeedUrl = "" }
    ValidationErrors = Map.empty
    Alert = None
  },
  Elmish.Cmd.none

let updateRequest (request: SubscribeToFeedRequest) (state: State) =
  let validationErrors =
    match request.Validate() with
    | Ok _ -> Map.empty
    | Error errors -> ValidationErrors.toMap errors

  {
    state with
        Request = request
        ValidationErrors = validationErrors
  }

let update (msg: Msg) (state: State) =
  match msg with
  | SetFeedName feedName ->
    let request = {
      state.Request with
          FeedName = feedName
    }

    updateRequest request state, Elmish.Cmd.none
  | SetFeedUrl feedUrl ->
    let request = { state.Request with FeedUrl = feedUrl }
    updateRequest request state, Elmish.Cmd.none
  | Subscribe ->
    let cmd =
      if state.ValidationErrors = Map.empty then
        Elmish.Cmd.OfAsync.either
          Remoting.securedServerApi.SubscribeToFeed
          state.Request
          SubscriptionResult
          GotException
      else
        Elmish.Cmd.none

    state, cmd
  | SubscriptionResult result ->
    match result with
    | Ok subscribedFeed ->
      state,
      Elmish.Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.AddFeedToContext subscribedFeed))
    | Error error ->
      match error with
      | AlreadySubscribed ->
        let alert =
          Danger {
            Reason = "You are already subscribed to that feed."
          }

        { state with Alert = Some alert }, Elmish.Cmd.none
  | DeleteFeed feedId ->
    state,
    Elmish.Cmd.OfAsync.either Remoting.securedServerApi.DeleteFeed { FeedId = feedId } DeleteFeedResult GotException
  | DeleteFeedResult(Ok(Deleted id)) ->
    state, Elmish.Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.DeleteFeedFromContext id))
  | DeleteFeedResult(Error error) ->
    let msg =
      match error with
      | DeleteFeedError.NotFound -> "That feed wasn't found"

    let alert = Danger { Reason = msg }
    { state with Alert = Some alert }, Elmish.Cmd.none
  | GotException exn ->
    let alert =
      Danger {
        Reason = "Something went wrong with that request!"
      }

    { state with Alert = Some alert }, Elmish.Cmd.none

let tableRow (deleteFeed: FeedId -> unit) (subscribedFeed: SubscribedFeed) =
  html
    $"""
    <tr>
      <th>{subscribedFeed.Name}</th>
      <td>{subscribedFeed.FeedUrl}</td>
      <td><button class="btn btn-primary btn-sm" @click={Ev(fun _ -> deleteFeed subscribedFeed.FeedId)}>Delete</button></td>
    </tr>
    """

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)
  let store = Hook.useStore ApplicationContext.store

  let showSubscriptionsModal () =
    let modalElement =
      document.getElementById ("subscriptions-modal") :?> Interop.HTMLDialogElement

    modalElement.showModal ()

  html
    $"""
    <div class="w-full flex flex-col">
      <div>
        <button class="btn btn-ghost" @click={Ev(fun _ -> showSubscriptionsModal ())}>
          <i class="fa-solid fa-add"></i>
          <span class="label-text normal-case">New Subscription</span>
        </button>
      </div>
      
      <dialog id="subscriptions-modal" class="modal card card-bordered bg-base-200 shadow-xl">
      {match state.Alert with
       | None -> Lit.nothing
       | Some alert -> Alerts.renderAlert alert}
       <form method="modal" class="modal-box">
          <div class="card-body">
            <span class="card-title">Subscribe to a Feed</span>
            <div class="form-control">
              <label for="feed-name" class="label">Feed name</label>
              <input id="feed-name" type="text" placeholder="feed name" class="input input-bordered" @keyup={EvVal(SetFeedName >> dispatch)} />
              {ValidationErrors.renderValidationErrors state.ValidationErrors "Feed name" state.Request.FeedName}
            </div>
            <div class="form-control">
              <label for="feed-url" class="label">Feed url</label>
              <input id="feed-url" type="text" placeholder="feed url" class="input input-bordered" @keyup={EvVal(SetFeedUrl >> dispatch)} />
              {ValidationErrors.renderValidationErrors state.ValidationErrors "Feed url" state.Request.FeedUrl}
            </div>
            <div class="card-actions">
              <button class="btn btn-primary w-full" @click={Ev(fun _ -> dispatch Subscribe)}>Subscribe</button>
            </div>
          </div>
        </form>
      </dialog>

      <div class="overflow-x-auto w-full h-full">
        <table class="table">
          <thead>
            <th>Feed name</th>
            <td>Feed url</td>
            <td>Actions</td>
          </thead>
          {store.SubscribedFeeds |> List.map (tableRow (DeleteFeed >> dispatch))}
        </table>
      </div>
    """
