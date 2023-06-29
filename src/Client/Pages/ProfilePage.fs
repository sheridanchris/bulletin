module ProfilePage

open Lit
open Lit.Elmish
open LitStore
open Shared

let private renderUser (user: UserModel) =
  html
    $"""
    <div class="w-screen h-screen flex flex-col items-center justify-center">
      <div class="card card-bordered shadow-xl bg-base-200">
        <div class="card-body items-center justify-center">
          <div class="avatar">
            <img class="w-24 h-24 rounded-full shadow-lg" src={user.ProfilePictureUrl} />
          </div>
          <span class="font-medium text-xl text-white">{user.Username}</span>
          <span>{user.EmailAddress}</span>
          <div class="card-actions">
            <a class="btn btn-primary btn-sm" href="/#/profile/edit">Edit profile</a>
            <a class="btn btn-primary btn-sm" href="/#/profile/edit/password">Change Password</a>
          </div>
        </div>
    </div>
  """

[<HookComponent>]
let Component () =
  let store = Hook.useStore (ApplicationContext.store)

  match store.User with
  | User user -> renderUser user
  | Anonymous -> Lit.nothing // TODO: ???
