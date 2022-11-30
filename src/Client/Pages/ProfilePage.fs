module ProfilePage

open Lit
open Lit.Elmish
open LitStore
open Shared

let private renderUser (user: UserModel) =
  html
    $"""
    <div class="min-h-screen flex flex-col items-center justify-center">
      <div class="w-full pt-2 max-w-sm bg-white border border-gray-200 rounded-lg shadow-md dark:bg-gray-800 dark:border-gray-700">
        <div class="flex flex-col items-center pb-10">
          <img class="w-24 h-24 mb-3 rounded-full shadow-lg" src={user.ProfilePictureUrl} alt="Rounded avatar"/>
          <h5 class="mb-1 text-xl font-medium text-gray-900 dark:text-white">{user.Username}</h5>
          <span class="text-sm text-gray-500 dark:text-gray-400">{user.EmailAddress}</span>
          <div class="flex mt-4 space-x-3 md:mt-6">
            <a href="/#/profile/edit" class="inline-flex items-center px-4 py-2 text-sm font-medium text-center text-white bg-blue-700 rounded-lg hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Edit profile</a>
            <a href="/#/profile/edit/password" class="inline-flex items-center px-4 py-2 text-sm font-medium text-center text-white bg-blue-700 rounded-lg hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Change password</a>
          </div>
        </div>
      </div>
    </div>
  """

[<HookComponent>]
let Component () =
  let store = Hook.useStore(ApplicationContext.store)

  match store.User with
  | User user -> renderUser user
  | Anonymous -> Lit.nothing // TODO: ???
