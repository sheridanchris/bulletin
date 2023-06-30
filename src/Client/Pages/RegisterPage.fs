module RegisterPage

open System
open Components
open Elmish
open Fable.Remoting.Client
open Shared
open Lit
open Lit.Elmish
open LitRouter
open Validus

type State = {
  CreateAccountRequest: CreateAccountRequest
  ValidationErrors: Map<string, string list>
  Alert: Alerts.Alert option
}

type Msg =
  | SetUsername of string
  | SetEmailAddress of string
  | SetPassword of string
  | SetConfirmPassword of string
  | Submit
  | GotResponse of Result<UserModel, CreateAccountError>
  | GotException of exn

let init () =
  {
    CreateAccountRequest = {
      Username = ""
      EmailAddress = ""
      Password = ""
      ConfirmPassword = ""
    }
    ValidationErrors = Map.empty
    Alert = None
  },
  Cmd.none

let updateCreateAccountRequest (createAccountRequest: CreateAccountRequest) (state: State) =
  let validationErrors =
    match createAccountRequest.Validate() with
    | Ok _ -> Map.empty
    | Error errors -> ValidationErrors.toMap errors

  {
    state with
        CreateAccountRequest = createAccountRequest
        ValidationErrors = validationErrors
  }

let update (msg: Msg) (state: State) =
  match msg with
  | SetUsername username ->
    let request = {
      state.CreateAccountRequest with
          Username = username
    }

    let newState = updateCreateAccountRequest request state
    newState, Cmd.none
  | SetEmailAddress emailAddress ->
    let request = {
      state.CreateAccountRequest with
          EmailAddress = emailAddress
    }

    let newState = updateCreateAccountRequest request state
    newState, Cmd.none
  | SetPassword password ->
    let request = {
      state.CreateAccountRequest with
          Password = password
    }

    let newState = updateCreateAccountRequest request state
    newState, Cmd.none
  | SetConfirmPassword confirmPassword ->
    let request = {
      state.CreateAccountRequest with
          ConfirmPassword = confirmPassword
    }

    let newState = updateCreateAccountRequest request state
    newState, Cmd.none
  | Submit ->
    let cmd =
      match state.CreateAccountRequest.Validate() with
      | Error _ -> Cmd.none
      | Ok request ->
        Cmd.OfAsync.either Remoting.unsecuredServerApi.CreateAccount state.CreateAccountRequest GotResponse GotException

    state, cmd
  | GotResponse(Ok userModel) ->
    state,
    Cmd.batch [
      Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.SetCurrentUser(User userModel)))
      Cmd.navigate "/"
    ]
  | GotResponse(Error creationError) ->
    let reason =
      match creationError with
      | UsernameTaken -> "That username is not available."
      | EmailAddressTaken -> "That email address is not available."

    let alert = Alerts.Danger { Reason = reason }
    { state with Alert = Some alert }, Cmd.none
  | GotException _ ->
    let alert =
      Alerts.Danger {
        Reason = "Something went wrong with that request!"
      }

    { state with Alert = Some alert }, Cmd.none

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  html
    $"""
    <div class="w-screen h-screen flex flex-col items-center justify-center">
      {match state.Alert with
       | None -> Lit.nothing
       | Some alert -> Alerts.renderAlert alert}
      <div class="card card-bordered shadow-xl bg-base-200 w-100">
        <div class="card-body">
          <span class="card-title">Create your account</span>
          <div class="form-control">
            <label for="username" class="label">Username</label>
            <input id="username" class="input input-bordered" placeholder="username" @keyup={EvVal(SetUsername >> dispatch)} />
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Username" state.CreateAccountRequest.Username}
          </div>
          <div class="form-control">
            <label for="email" class="label">Email Address</label>
            <input id="email" type="email" class="input input-bordered" placeholder="email address" @keyup={EvVal(SetEmailAddress >> dispatch)} />
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Email address" state.CreateAccountRequest.EmailAddress}
          </div>
          <div class="form-control">
            <label for="password" class="label">Password</label>
            <input id="password" type="password" class="input input-bordered" placeholder="••••••••" @keyup={EvVal(SetPassword >> dispatch)} />
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Password" state.CreateAccountRequest.Password}
          </div>
          <div class="form-control">
            <label for="confirm-password" class="label">Confirm Password</label>
            <input id="confirm-password" type="password" class="input input-bordered" placeholder="••••••••" @keyup={EvVal(SetConfirmPassword >> dispatch)} />
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Confirmation" state.CreateAccountRequest.ConfirmPassword}
          </div>
          <div class="card-actions">
            <button class="btn btn-primary w-full" @click={Ev(fun _ -> dispatch Submit)}>Create Account</button>
            <span>Already Have an Account? <a class="link link-hover link-secondary" href="/#/login">Login</a></span>
          </div>
        </div>
      </div>
    """
