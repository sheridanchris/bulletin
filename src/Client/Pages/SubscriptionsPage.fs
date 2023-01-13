module SubscriptionsPage

open System
open Alerts
open Lit
open Lit.Elmish
open LitStore
open Shared
open ValidatedInput
open Validus

type State = {
  FeedName: ValidationState<string>
  FeedUrl: ValidationState<string>
  Alert: Alert
}

type Msg =
  | SetFeedName of string
  | SetFeedUrl of string
  | Subscribe
  | DeleteFeed of Guid
  | SubscriptionResult of Result<SubscribedFeed, SubscribeToFeedError>
  | DeleteFeedResult of Result<DeleteFeedResponse, DeleteFeedError>

let init () =
  {
    FeedName = ValidationState.createInvalidWithNoErrors "Feed name" String.Empty
    FeedUrl = ValidationState.createInvalidWithNoErrors "Feed url" String.Empty
    Alert = NothingToWorryAbout
  },
  Elmish.Cmd.none

let update (msg: Msg) (state: State) =
  match msg with
  | SetFeedName feedName ->
    let feedNameState =
      ValidationState.create (Validators.stringNotEmptyValidator "Feed name") feedName

    { state with FeedName = feedNameState }, Elmish.Cmd.none
  | SetFeedUrl feedUrl ->
    // TODO: Check for valid url?
    let feedUrlState =
      ValidationState.create (Validators.stringNotEmptyValidator "Feed url") feedUrl

    { state with FeedUrl = feedUrlState }, Elmish.Cmd.none
  | Subscribe ->
    let cmd =
      match state.FeedName, state.FeedUrl with
      | Valid feedName, Valid feedUrl ->
        Elmish.Cmd.OfAsync.perform
          Remoting.securedServerApi.SubscribeToFeed
          {
            FeedName = feedName
            FeedUrl = feedUrl
          }
          SubscriptionResult
      | _ -> Elmish.Cmd.none

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

        { state with Alert = alert }, Elmish.Cmd.none
  | DeleteFeed feedId ->
    state, Elmish.Cmd.OfAsync.perform Remoting.securedServerApi.DeleteFeed { FeedId = feedId } DeleteFeedResult
  | DeleteFeedResult(Ok(Deleted id)) ->
    state, Elmish.Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.DeleteFeedFromContext id))
  | DeleteFeedResult(Error _) -> state, Elmish.Cmd.none

let tableRow (deleteFeed: Guid -> unit) (subscribedFeed: SubscribedFeed) =
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
            @change={EvVal(SetFeedName >> dispatch)} placeholder="feed name" type="text" id="feed-name"
            class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500
            block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500
            dark:focus:border-blue-500" />
        </div>
        <div class="mb-6" colspan="3">
          <label for="feed-url" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">RSS feed url</label>
          <input
            @change={EvVal(SetFeedUrl >> dispatch)} placeholder="feed url" type="text" id="feed-url"
            class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block
            w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500
            dark:focus:border-blue-500" />
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
