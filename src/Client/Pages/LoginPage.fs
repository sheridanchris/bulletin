module LoginPage

open System
open Components
open Components.Alerts
open Elmish
open Fable.Core
open Lit
open Lit.Elmish
open LitStore
open LitRouter
open Shared
open Validus

type State = {
  Request: LoginRequest
  ValidationErrors: Map<string, string list>
  Alert: Alert option
}

type Msg =
  | SetUsername of string
  | SetPassword of string
  | Submit
  | GotLoginResponse of Result<UserModel, LoginError>
  | GotException of exn

let init () =
  {
    Request = { Username = ""; Password = "" }
    ValidationErrors = Map.empty
    Alert = None
  },
  Cmd.none

let updateRequest (request: LoginRequest) (state: State) =
  let validationErrors =
    match request.Validate() with
    | Ok _ -> Map.empty
    | Error error -> ValidationErrors.toMap error

  {
    state with
        Request = request
        ValidationErrors = validationErrors
  }

let update (msg: Msg) (state: State) =
  match msg with
  | SetUsername username ->
    let request = {
      state.Request with
          Username = username
    }

    updateRequest request state, Cmd.none
  | SetPassword password ->
    let request = {
      state.Request with
          Password = password
    }

    updateRequest request state, Cmd.none
  | Submit ->
    let cmd =
      if state.ValidationErrors = Map.empty then
        Cmd.OfAsync.either Remoting.unsecuredServerApi.Login state.Request GotLoginResponse GotException
      else
        Cmd.none

    state, cmd
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
        Danger {
          Reason = "Invalid username and/or password!"
        }

      { state with Alert = Some alert }, Cmd.none
  | GotException exn ->
    let alert =
      Danger {
        Reason = "Something went wrong with that request!"
      }

    { state with Alert = Some alert }, Cmd.none

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  html
    $"""
    <div class="flex flex-col items-center justify-center w-screen h-screen">
      {match state.Alert with
       | None -> Lit.nothing
       | Some alert -> Alerts.renderAlert alert}
       <div class="card card-bordered shadow-xl bg-base-200 w-100">
        <div class="card-body">
          <span class="card-title">Sign in to your account</span>
          <div class="form-control">
            <label for="username" class="label">Username</label>
            <input id="username" class="input input-bordered" placeholder="username" @keyup={EvVal(SetUsername >> dispatch)} />
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Username" state.Request.Username}
          </div>
          <div class="form-control">
            <label for="password" class="label">Password</label>
            <input id="password" type="password" class="input input-bordered" placeholder="••••••••" @keyup={EvVal(SetPassword >> dispatch)} />
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Password" state.Request.Password}
          </div>
          <div class="card-actions pt-2">
            <button class="btn btn-primary btn-md w-full" @click={Ev(fun _ -> dispatch Submit)}>Login</button>
            <span>Not Registered? <a class="link link-hover link-secondary" href="/#/register">Create an account</a></span>
          </div>
        </div>
      </div>
    </div>
    """
