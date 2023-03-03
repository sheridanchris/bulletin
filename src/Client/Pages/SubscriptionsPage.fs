module SubscriptionsPage

open System
open Components
open Components.Alerts
open Lit
open Lit.Elmish
open LitStore
open Shared
open Validus

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

  { state with
      Request = request
      ValidationErrors = validationErrors
  }

let update (msg: Msg) (state: State) =
  match msg with
  | SetFeedName feedName ->
    let request =
      { state.Request with
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
          Danger
            {
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
      Danger
        {
          Reason = "Something went wrong with that request!"
        }

    { state with Alert = Some alert }, Elmish.Cmd.none

let tableRow (deleteFeed: FeedId -> unit) (subscribedFeed: SubscribedFeed) =
  html
    $"""
    <tr class="bg-white border-b dark:bg-gray-800 dark:border-gray-700">
      <th scope="row" class="py-4 px-6 font-medium text-gray-900 whitespace-nowrap dark:text-white">
          {subscribedFeed.Name}
      </th>
      <td class="py-4 px-6">
          {subscribedFeed.FeedUrl}
      </td>
      <td class="py-4 px-6">
          <button
            @click={Ev(fun _ -> deleteFeed subscribedFeed.FeedId)}
            class="text-white bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300
            font-medium rounded-lg text-sm px-5 py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700
            dark:focus:ring-blue-800">Delete</button>
      </td>
    </tr>
    """

// TODO: These validation errors don't display properly.
[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)
  let store = Hook.useStore ApplicationContext.store

  html
    $"""
    <div class="w-full flex flex-col gap-y-3 justify-center items-center pt-20">
      {AlertComponent state.Alert}
      <div class="flex flex-col sm:flex-row justify-center items-center w-full gap-x-3">
        <div class="mb-6" colspan="3">
          <label for="feed-name" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Feed name</label>
          <input
            @keyup={EvVal(SetFeedName >> dispatch)} placeholder="feed name" type="text" id="feed-name"
            class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500
            block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500
            dark:focus:border-blue-500" />
          {ValidationErrors.renderValidationErrors state.ValidationErrors "Feed name" state.Request.FeedName}
        </div>
        <div class="mb-6" colspan="3">
          <label for="feed-url" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">RSS feed url</label>
          <input
            @keyup={EvVal(SetFeedUrl >> dispatch)} placeholder="feed url" type="text" id="feed-url"
            class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block
            w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500
            dark:focus:border-blue-500" />
          {ValidationErrors.renderValidationErrors state.ValidationErrors "Feed url" state.Request.FeedUrl}
        </div>
        <button
          @click={Ev(fun _ -> dispatch Subscribe)}
          class="text-white bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg
          text-sm px-5 py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Subscribe</button>
      </div>
      <div class="overflow-x-auto relative">
        <table class="w-full text-sm text-left text-gray-500 dark:text-gray-400">
          <thead class="text-xs text-gray-700 uppercase bg-gray-50 dark:bg-gray-700 dark:text-gray-400">
            <tr>
              <th scope="col" class="py-3 px-6">
                  Feed
              </th>
              <th scope="col" class="py-3 px-6">
                  RSS Url
              </th>
              <th scope="col" class="py-3 px-6">
                  Actions
              </th>
            </tr>
          </thead>
          <tbody>
            {store.SubscribedFeeds |> List.map (tableRow (DeleteFeed >> dispatch))}
          </tbody>
        </table>
      </div>
    </div>
    """
