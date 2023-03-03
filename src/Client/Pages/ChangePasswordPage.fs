module ChangePasswordPage

open Components
open Components.Alerts
open Lit
open Lit.Elmish
open Shared
open Validus

type State = {
  Request: ChangePasswordRequest
  ValidationErrors: Map<string, string list>
  Alert: Alert option
}

type Msg =
  | SetCurrentPassword of string
  | SetNewPassword of string
  | Submit
  | GotResult of Result<unit, ChangePasswordError>
  | GotException of exn

let init () =
  {
    Request =
      {
        CurrentPassword = ""
        NewPassword = ""
      }
    ValidationErrors = Map.empty
    Alert = None
  },
  Elmish.Cmd.none

let updateRequest (request: ChangePasswordRequest) (state: State) =
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
  | SetCurrentPassword password ->
    let request =
      { state.Request with
          CurrentPassword = password
      }

    updateRequest request state, Elmish.Cmd.none
  | SetNewPassword password ->
    let request =
      { state.Request with
          NewPassword = password
      }

    updateRequest request state, Elmish.Cmd.none
  | Submit ->
    let cmd =
      if state.ValidationErrors = Map.empty then
        Elmish.Cmd.OfAsync.either Remoting.securedServerApi.ChangePassword state.Request GotResult GotException
      else
        Elmish.Cmd.none

    state, cmd
  | GotResult result ->
    let alert =
      match result with
      | Ok() ->
        Success
          {
            Reason = "Your password has been changed"
          }
      | Error error ->
        let reason =
          match error with
          | PasswordsDontMatch -> "Your current password is invalid."
          | UserNotFound -> "Oops, something went wrong. Please refresh the page and try again!"

        Danger { Reason = reason }

    { state with Alert = Some alert }, Elmish.Cmd.none
  | GotException _ ->
    let alert =
      Danger
        {
          Reason = "Oops, something went wrong. Please refresh the page and try again!"
        }

    { state with Alert = Some alert }, Elmish.Cmd.none

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  html
    $"""
    <div class="min-h-screen flex flex-col items-center justify-center">
      {AlertComponent state.Alert}
      <div class="w-full max-w-sm p-4 bg-white border border-gray-200 rounded-lg shadow-md sm:p-6 md:p-8 dark:bg-gray-800 dark:border-gray-700">
        <div class="space-y-6" action="#">
          <h5 class="text-xl font-medium text-gray-900 dark:text-white">Change your password</h5>
          <div>
            <label for="current-password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your current password</label>
            <input @keyup={EvVal(SetCurrentPassword >> dispatch)} type="password" name="current-password" id="current-password"
            placeholder="••••••••" class="bg-gray-50 border border-gray-300 text-gray-900
            text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full
            p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white">
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Current password" state.Request.CurrentPassword}
          </div>
          <div>
            <label for="new-password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your new password</label>
            <input @keyup={EvVal(SetNewPassword >> dispatch)} type="password" name="new-password" id="new-password" placeholder="••••••••"
            class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg
            focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600
            dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" />
            {ValidationErrors.renderValidationErrors state.ValidationErrors "New password" state.Request.NewPassword}
          </div>
          <button @click={Ev(fun _ -> dispatch Submit)} class="w-full text-white bg-blue-700 hover:bg-blue-800 focus:ring-4
            focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-5
            py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Submit</button>
        </div>
      </div>
    </div>
    """
