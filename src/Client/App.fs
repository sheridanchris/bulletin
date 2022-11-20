module App

open Lit
open LitRouter
open LitStore
open Fable.Core.JsInterop
open Shared

importSideEffects "./index.css"

// TODO: Is there anyway to require authentication for navigation???

type Page =
  | Login
  | Register
  | Feed
  | Subscriptions
  | NotFound

let parseUrl =
  function
  | []
  | [ "feed" ] -> Feed
  | [ "login" ] -> Login
  | [ "register" ] -> Register
  | [ "subscriptions" ] -> Subscriptions
  | _ -> NotFound

[<LitElement("my-app")>]
let MyApp () =
  let _ = LitElement.init (fun cfg -> cfg.useShadowDom <- false)
  let store = Hook.useStore ApplicationContext.store
  let path = Hook.useRouter RouteMode.Hash

  let renderAnonymous () =
    html
      $"""
      <a href="/#/login">Login</a>
      <a href="/#/register">Register</a>
      """

  let renderUser (_: UserModel) =
    html
      $"""
      <a href="/#/feed">View your Feed</a>
      <a href="/#/subscriptions">Manage Your Subscriptions</a>
      """

  html
    $"""
    <nav class="flex w-full">
      <div class="mr-auto">
        <a href="/#/">
          <h1 class="text-lg">Bulletin</h1>
        </a>
      </div>
      <div class="ml-auto mr-3 flex gap-x-3">
        {match store.User with
         | Anonymous -> renderAnonymous ()
         | User user -> renderUser user}
       </div>
    </nav>
    {match parseUrl path with
     | Login -> LoginPage.Component()
     | Register -> RegisterPage.Component()
     | Feed -> FeedPage.Component()
     | Subscriptions -> SubscriptionsPage.Component()
     | NotFound -> NotFoundPage.Component()}
    """
