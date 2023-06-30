module EditProfilePage

open System
open Components
open Components.Alerts
open Lit
open Lit.Elmish
open LitRouter
open LitStore
open Shared
open Validus

type State = {
  Request: EditUserProfileRequest
  ValidationErrors: Map<string, string list>
  Alert: Alert option
}

type Msg =
  | SetUsername of string
  | SetEmailAddress of string
  | SetGravatarEmailAddress of string
  | Submit
  | GotResult of Result<UserModel, EditUserProfileError>
  | GotException of exn

let init () =
  {
    Request = {
      Username = None
      EmailAddress = None
      GravatarEmailAddress = None
    }
    ValidationErrors = Map.empty
    Alert = None
  },
  Elmish.Cmd.none

let calculateInputValue value =
  if String.IsNullOrWhiteSpace value then None else Some value

let updateRequest (request: EditUserProfileRequest) (state: State) =
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
  | SetUsername username ->
    let request = {
      state.Request with
          Username = calculateInputValue username
    }

    updateRequest request state, Elmish.Cmd.none
  | SetEmailAddress emailAddress ->
    let request = {
      state.Request with
          EmailAddress = calculateInputValue emailAddress
    }

    updateRequest request state, Elmish.Cmd.none
  | SetGravatarEmailAddress emailAddress ->
    let request = {
      state.Request with
          GravatarEmailAddress = calculateInputValue emailAddress
    }

    updateRequest request state, Elmish.Cmd.none
  | Submit ->
    let cmd =
      if state.ValidationErrors = Map.empty then
        Elmish.Cmd.OfAsync.either Remoting.securedServerApi.EditUserProfile state.Request GotResult GotException
      else
        Elmish.Cmd.none

    state, cmd
  | GotResult(Ok userModel) ->
    state,
    Elmish.Cmd.batch [
      Elmish.Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.SetCurrentUser(User userModel)))
      Cmd.navigate "profile"
    ]
  | GotResult(Error _)
  | GotException _ ->
    let alert =
      Danger {
        Reason = "Failed to edit your profile."
      }

    { state with Alert = Some alert }, Elmish.Cmd.none

// TODO: Card needs to be bigger (width)
let renderUser (state: State) (dispatch: Msg -> unit) (user: UserModel) =
  html
    $"""
    <div class="h-screen w-screen flex flex-col items-center justify-center">
      {match state.Alert with
       | None -> Lit.nothing
       | Some alert -> Alerts.renderAlert alert}
       <div class="card card-bordered shadow-xl bg-base-200">
        <div class="card-body">
          <span class="card-title">Edit Your Profile</span>
          <div class="form-control">
            <label for="username" class="label">Username</label>
            <input id="username" class="input input-bordered" .value={user.Username} @keyup={EvVal(SetUsername >> dispatch)} />
            {ValidationErrors.renderValidationErrors
               state.ValidationErrors
               "Username"
               (state.Request.Username |> Option.defaultValue "")}
          </div>
          <div class="form-control">
            <label for="email-address" class="label">Email Address</label>
            <input id="email-address" type="email" class="input input-bordered" .value={user.EmailAddress} @keyup={EvVal(SetEmailAddress >> dispatch)} />
            {ValidationErrors.renderValidationErrors
               state.ValidationErrors
               "Email address"
               (state.Request.EmailAddress |> Option.defaultValue "")}
          </div>
          <div class="form-control">
            <label for="gravatar-email-address" class="label">Gravatar Email Address</label>
            <input id="gravatar-email-address" type="email" class="input input-bordered" .value={user.GravatarEmailAddress} @keyup={EvVal(SetGravatarEmailAddress >> dispatch)} />
            {ValidationErrors.renderValidationErrors
               state.ValidationErrors
               "Gravatar email address"
               (state.Request.GravatarEmailAddress |> Option.defaultValue "")}
          </div>
          <div class="card-actions">
            <button @click={Ev(fun _ -> dispatch Submit)} class="btn btn-primary w-full">Edit Your Profile</button>
          </div>
        </div>
      </div>
    </div>
    """

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)
  let store = Hook.useStore ApplicationContext.store

  match store.User with
  | Anonymous -> Lit.nothing
  | User user -> renderUser state dispatch user
