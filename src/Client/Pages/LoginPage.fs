module LoginPage

open System
open Elmish
open Fable.Core
open Lit
open Lit.Elmish
open LitStore
open LitRouter
open Shared

type State = {
  Username: string
  Password: string
  Error: string
}

type Msg =
  | SetUsername of string
  | SetPassword of string
  | Submit
  | GotLoginResponse of Result<UserModel, LoginError>

let init () =
  {
    Username = ""
    Password = ""
    Error = ""
  },
  Cmd.none

let update (msg: Msg) (state: State) =
  match msg with
  | SetUsername username -> { state with Username = username }, Cmd.none
  | SetPassword password -> { state with Password = password }, Cmd.none
  | Submit ->
    state,
    Cmd.OfAsync.perform
      Remoting.unsecuredServerApi.Login
      {
        Username = state.Username
        Password = state.Password
      }
      GotLoginResponse
  | GotLoginResponse(Ok user) ->
    state,
    Cmd.batch [
      Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.LoggedIn user))
      Cmd.navigate "/"
    ]
  | GotLoginResponse(Error loginError) ->
    match loginError with
    | InvalidUsernameAndOrPassword -> { state with Error = "Invalid username and/or password." }, Cmd.none

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  let renderError errorMsg =
    html $"""<p class="text-red-500">{errorMsg}</p>"""

  html
    $"""
    <div class="min-h-screen flex items-center justify-center">
      <div class="w-full max-w-sm p-4 bg-white border border-gray-200 rounded-lg shadow-md sm:p-6 md:p-8 dark:bg-gray-800 dark:border-gray-700">
        <div class="space-y-6" action="#">
          <h5 class="text-xl font-medium text-gray-900 dark:text-white">Sign in to your account</h5>
          <div>
            <label for="username" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your username</label>
            <input @change={EvVal(SetUsername >> dispatch)} type="text" name="username" id="username" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" placeholder="username" required>
          </div>
          <div>
            <label for="password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your password</label>
            <input @change={EvVal(SetPassword >> dispatch)} type="password" name="password" id="password" placeholder="••••••••" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" required>
          </div>
          <button @click={Ev(fun _ -> dispatch Submit)} class="w-full text-white bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-5 py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Login</button>
          <div class="text-sm font-medium text-gray-500 dark:text-gray-300">
            Not registered? <a href="/#/register" class="text-blue-700 hover:underline dark:text-blue-500">Create account</a>
          </div>
        </div>
      </div>
    </div>
    """
