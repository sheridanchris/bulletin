module ChangePasswordPage

open System
open Alerts
open Lit
open Lit.Elmish
open ValidatedInput
open Shared

type State = {
  CurrentPassword: string
  NewPassword: ValidationState<string>
  Alert: Alert
}

type Msg =
  | SetCurrentPassword of string
  | SetNewPassword of string
  | Submit
  | GotResult of Result<unit, ChangePasswordError>

let init () =
  {
    CurrentPassword = String.Empty
    NewPassword = ValidationState.createInvalidWithNoErrors "New password" String.Empty
    Alert = NothingToWorryAbout
  },
  Elmish.Cmd.none

let update (msg: Msg) (state: State) =
  match msg with
  | SetCurrentPassword password -> { state with CurrentPassword = password }, Elmish.Cmd.none
  | SetNewPassword password ->
    let newPasswordState =
      ValidationState.create (Validators.passwordValidator "New password") password

    { state with NewPassword = newPasswordState }, Elmish.Cmd.none
  | Submit ->
    match state.NewPassword with
    | Invalid _ -> state, Elmish.Cmd.none
    | Valid newPassword ->
      state,
      Elmish.Cmd.OfAsync.perform
        Remoting.securedServerApi.ChangePassword
        {
          CurrentPassword = state.CurrentPassword
          NewPassword = newPassword
        }
        GotResult
  | GotResult result ->
    let alert =
      match result with
      | Ok() -> Success { Reason = "Your password has been changed" }
      | Error error ->
        let reason =
          match error with
          | PasswordsDontMatch -> "Your current password is invalid."
          | UserNotFound -> "Oops, something went wrong. Please refresh the page and try again!"

        Danger { Reason = reason }

    { state with Alert = alert }, Elmish.Cmd.none

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
            <input @change={EvVal(SetCurrentPassword >> dispatch)} type="password" name="current-password" id="current-password"
            placeholder="••••••••" class="bg-gray-50 border border-gray-300 text-gray-900
            text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full
            p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white">
          </div>
          <div>
            <label for="new-password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your new password</label>
            <input @change={EvVal(SetNewPassword >> dispatch)} type="password" name="new-password" id="new-password" placeholder="••••••••"
            class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg
            focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600
            dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" />
            {ErrorComponent "text-sm text-red-500" "New password" state.NewPassword}
          </div>
          <button @click={Ev(fun _ -> dispatch Submit)} class="w-full text-white bg-blue-700 hover:bg-blue-800 focus:ring-4
            focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-5
            py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Submit</button>
        </div>
      </div>
    </div>
    """
