module LoginPage

open System
open Alerts
open Elmish
open Fable.Core
open Lit
open Lit.Elmish
open LitStore
open LitRouter
open Shared
open Validus
open ValidatedInput

type State = {
  Username: ValidationState<string>
  Password: ValidationState<string>
  Alert: Alert
}

type Msg =
  | SetUsername of string
  | SetPassword of string
  | Submit
  | GotLoginResponse of Result<UserModel, LoginError>

let init () =
  {
    Username = ValidationState.createInvalidWithNoErrors "Username" String.Empty
    Password = ValidationState.createInvalidWithNoErrors "Password" String.Empty
    Alert = NothingToWorryAbout
  },
  Cmd.none

let update (msg: Msg) (state: State) =
  match msg with
  | SetUsername username ->
    { state with
        Username = ValidationState.create (Validators.stringNotEmptyValidator "Username") username
    },
    Cmd.none
  | SetPassword password ->
    { state with
        Password = ValidationState.create (Validators.stringNotEmptyValidator "Password") password
    },
    Cmd.none
  | Submit ->
    match state.Username, state.Password with
    | Valid username, Valid password ->
      state,
      Cmd.OfAsync.perform
        Remoting.unsecuredServerApi.Login
        {
          Username = username
          Password = password
        }
        GotLoginResponse
    | _ -> state, Elmish.Cmd.none
  | GotLoginResponse(Ok user) ->
    state,
    Cmd.batch [
      Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.SetCurrentUser(User user)))
      Cmd.navigate "feed"
    ]
  | GotLoginResponse(Error loginError) ->
    match loginError with
    | InvalidUsernameAndOrPassword ->
      let alert =
        Danger
          {
            Reason = "Invalid username and/or password."
          }

      { state with Alert = alert }, Cmd.none

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  html
    $"""
    <div class="min-h-screen flex flex-col items-center justify-center">
      {AlertComponent state.Alert}
      <div class="w-full max-w-sm p-4 bg-white border border-gray-200 rounded-lg shadow-md sm:p-6 md:p-8 dark:bg-gray-800 dark:border-gray-700">
        <div class="space-y-6" action="#">
          <h5 class="text-xl font-medium text-gray-900 dark:text-white">Sign in to your account</h5>
          <div>
            <label for="username" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your username</label>
            <input @keyup={EvVal(SetUsername >> dispatch)} type="text" name="username" id="username" class="bg-gray-50
            border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500
            focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500
            dark:placeholder-gray-400 dark:text-white" placeholder="username" />
            {ErrorComponent "text-sm text-red-500" "Username" state.Username}
          </div>
          <div>
            <label for="password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your password</label>
            <input @keyup={EvVal(SetPassword >> dispatch)} type="password" name="password" id="password" placeholder="••••••••"
            class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg
            focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600
            dark:border-gray-500 dark:placeholder-gray-400 dark:text-white"> {ErrorComponent "text-sm text-red-500" "Password" state.Password}
          </div>
          <button @click={Ev(fun _ -> dispatch Submit)} class="w-full text-white bg-blue-700 hover:bg-blue-800 focus:ring-4
            focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-5
            py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Login</button>
          <div class="text-sm font-medium text-gray-500 dark:text-gray-300">
            Not registered? <a href="/#/register" class="text-blue-700 hover:underline dark:text-blue-500">Create account</a>
          </div>
        </div>
      </div>
    </div>
    """
