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
    Request = {
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

  {
    state with
        Request = request
        ValidationErrors = validationErrors
  }

let update (msg: Msg) (state: State) =
  match msg with
  | SetCurrentPassword password ->
    let request = {
      state.Request with
          CurrentPassword = password
    }

    updateRequest request state, Elmish.Cmd.none
  | SetNewPassword password ->
    let request = {
      state.Request with
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
        Success {
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
      Danger {
        Reason = "Oops, something went wrong. Please refresh the page and try again!"
      }

    { state with Alert = Some alert }, Elmish.Cmd.none

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  html
    $"""
    <div class="w-screen h-screen flex flex-col items-center justify-center">
      {match state.Alert with
       | None -> Lit.nothing
       | Some alert -> Alerts.renderAlert alert}
       <div class="card card-bordered shadow-xl bg-base-200">
        <div class="card-body">
          <span class="card-title">Change Your Password</span>
          <div class="form-action">
            <label for="current-password" class="label">Current password</label>
            <input id="current-password" type="password" class="input input-bordered" placeholder="••••••••" @keyup={EvVal(SetCurrentPassword >> dispatch)} />
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Current password" state.Request.CurrentPassword}
          </div>
          <div class="form-action">
            <label for="new-password" class="label">New password</label>
            <input id="new-password" type="password" class="input input-bordered" placeholder="••••••••" @keyup={EvVal(SetNewPassword >> dispatch)} />
            {ValidationErrors.renderValidationErrors state.ValidationErrors "New password" state.Request.NewPassword}
          </div>
          <div class="card-actions">
            <button @click={Ev(fun _ -> dispatch Submit)} class="btn btn-primary w-full">Change Password</button>
          </div>
        </div>
      </div>
    </div>
    """
