module App

open ApplicationContext
open Lit
open LitRouter
open LitStore
open Fable.Core.JsInterop
open Routing
open Shared

// TODO: Add theme switcher.
// TODO: Add unauthorized page.

importSideEffects "./index.css"

[<LitElement("my-app")>]
let App () =
  let _ = LitElement.init (fun cfg -> cfg.useShadowDom <- false)
  let store = Hook.useStore ApplicationContext.store
  let page = Page.parseUrl (Hook.useRouter RouteMode.Hash)

  let renderAnonymous () =
    html
      $"""
      <a class="btn btn-primary" href="/#/login">Login</a>
      """

  let renderNavigation () =
    html
      $"""
      <div class="navbar-center">
        <ul class="menu menu-horizontal">
          <li><a href="/#/">Home</a></li>
          <li><a href="/#/feed">Feed</a></li>
          <li><a href="/#/subscriptions">Subscriptions</a></li>
        </ul>
      </div>
      """

  let renderUser (user: UserModel) =
    html
      $"""
      <div class="dropdown dropdown-end">
        <label tabindex="0" class="btn btn-ghost btn-circle avatar">
          <div class="w-10 rounded-full">
            <img src="{user.ProfilePictureUrl}" />
          </div>
        </label>
        <ul tabindex="0" class="menu menu-sm dropdown-content mt-3 p-2 shadow bg-base-100 rounded-box w-52">
          <li><a href="/#/profile">Profile</a></li>
          <li><a href="/#/logout">Logout</a></li>
        </ul>
      </div>
      """

  let dataTheme theme =
    match theme with
    | Light -> "light"
    | Dark -> "dark"

  let nextThemeIcon theme =
    match theme with
    | Light -> "fa-solid fa-moon"
    | Dark -> "fa-solid fa-sun"

  html
    $"""
    <div class="w-screen min-h-screen" data-theme="{dataTheme store.CurrentTheme}">
      <div class="navbar bg-base-100">
        <div class="navbar-start">
          <a class="btn btn-ghost normal-case text-xl" href="/#/">Bulletin</a>
        </div>
        {match store.User with
         | Anonymous -> Lit.nothing
         | User _ -> renderNavigation ()}
        <div class="navbar-end">
          <button class="btn btn-ghost" @click={Ev(fun _ -> ApplicationContext.dispatch ApplicationContext.ToggleTheme)}>
            <i class="{nextThemeIcon store.CurrentTheme}"></i>
          </button>
          {match store.User with
           | Anonymous -> renderAnonymous ()
           | User user -> renderUser user}
        </div>
      </div>
      {match page with
       | Home -> HomePage.Component()
       | Login -> LoginPage.Component()
       | Register -> RegisterPage.Component()
       | Feed -> FeedPage.Component()
       | Subscriptions -> SubscriptionsPage.Component()
       | Profile -> ProfilePage.Component()
       | EditProfile -> EditProfilePage.Component()
       | ChangePassword -> ChangePasswordPage.Component()
       | Page.NotFound -> NotFoundPage.Component()}
     </div>
    """
