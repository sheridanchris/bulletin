module SubscriptionsPage

open Lit
open Lit.Elmish
open Shared

// TODO: Delete feed.

type State = {
  FeedName: string
  FeedUrl: string
  SubscribedFeeds: SubscribedFeed list
}

type Msg =
  | SetFeedName of string
  | SetFeedUrl of string
  | SetSubscribedFeeds of SubscribedFeed list
  | Subscribe
  | SubscriptionResult of Result<SubscribedFeed, SubscribeToFeedError>

let init () =
  {
    FeedName = ""
    FeedUrl = ""
    SubscribedFeeds = []
  },
  Elmish.Cmd.OfAsync.perform Remoting.securedServerApi.GetSubscribedFeeds () SetSubscribedFeeds

let update (msg: Msg) (state: State) =
  match msg with
  | SetFeedName feedName -> { state with FeedName = feedName }, Elmish.Cmd.none
  | SetFeedUrl feedUrl -> { state with FeedUrl = feedUrl }, Elmish.Cmd.none
  | SetSubscribedFeeds subscribedFeeds -> { state with SubscribedFeeds = subscribedFeeds }, Elmish.Cmd.none
  | Subscribe ->
    state,
    Elmish.Cmd.OfAsync.perform
      Remoting.securedServerApi.SubscribeToFeed
      {
        FeedName = state.FeedName
        FeedUrl = state.FeedUrl
      }
      SubscriptionResult
  | SubscriptionResult result ->
    match result with
    | Ok subscribedFeed -> { state with SubscribedFeeds = subscribedFeed :: state.SubscribedFeeds }, Elmish.Cmd.none
    | Error error -> state, Elmish.Cmd.none // TODO: this.

let tableRow (subscribedFeed: SubscribedFeed) =
  html
    $"""
    <tr>
      <td>{subscribedFeed.Name}</td>
      <td>{subscribedFeed.FeedUrl}</td>
      <td>
        <button>Delete</button>
      </td>
    </tr>
    """

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  html
    $"""
    <input placeholder="feed name" @change={EvVal(SetFeedName >> dispatch)} />
    <input placeholder="feed url" @change={EvVal(SetFeedUrl >> dispatch)} />
    <button @click={Ev(fun _ -> dispatch Subscribe)}>Subscribe</button>
    <table>
      <tr>
        <th>Feed Name</th>
        <th>Feed Url</th>
        <th>Actions</th>
      </tr>
      {state.SubscribedFeeds |> List.map tableRow}
    </table>
    """
