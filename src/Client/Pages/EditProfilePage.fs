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
    Request =
      {
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

  { state with
      Request = request
      ValidationErrors = validationErrors
  }

let update (msg: Msg) (state: State) =
  match msg with
  | SetUsername username ->
    let request =
      { state.Request with
          Username = calculateInputValue username
      }

    updateRequest request state, Elmish.Cmd.none
  | SetEmailAddress emailAddress ->
    let request =
      { state.Request with
          EmailAddress = calculateInputValue emailAddress
      }

    updateRequest request state, Elmish.Cmd.none
  | SetGravatarEmailAddress emailAddress ->
    let request =
      { state.Request with
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
      Danger
        {
          Reason = "Failed to edit your profile."
        }

    { state with Alert = Some alert }, Elmish.Cmd.none

let renderUser (state: State) (dispatch: Msg -> unit) (user: UserModel) =
  html
    $"""
    <div class="min-h-screen flex flex-col items-center justify-center">
      {AlertComponent state.Alert}
      <div class="w-full max-w-sm p-4 bg-white border border-gray-200 rounded-lg shadow-md sm:p-6 md:p-8 dark:bg-gray-800 dark:border-gray-700">
        <div class="space-y-6" action="#">
          <h5 class="text-xl font-medium text-gray-900 dark:text-white">Edit your profile</h5>
          <div>
            <label for="username" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your username</label>
            <input .value={user.Username} @keyup={EvVal(SetUsername >> dispatch)} type="text" name="username" id="username" class="bg-gray-50
            border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500
            focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500
            dark:placeholder-gray-400 dark:text-white" placeholder="username" />
            {ValidationErrors.renderValidationErrors
               state.ValidationErrors
               "Username"
               (state.Request.Username |> Option.defaultValue "")}
          </div>
          <div>
            <label for="email-address" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your email address</label>
            <input
              .value={user.EmailAddress} @keyup={EvVal(SetEmailAddress >> dispatch)}
              type="text" name="email-address" id="email-address"
              class="bg-gray-50
              border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500
              focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500
              dark:placeholder-gray-400 dark:text-white" placeholder="email address" />
            {ValidationErrors.renderValidationErrors
               state.ValidationErrors
               "Email address"
               (state.Request.EmailAddress |> Option.defaultValue "")}
          </div>
          <div>
            <label for="gravatar-email-address" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your gravatar email address</label>
            <input
              .value={user.GravatarEmailAddress}
              @keyup={EvVal(SetGravatarEmailAddress >> dispatch)}
              type="text"
              name="gravatar-email-address"
              id="gravatar-email-address"
              class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg
              focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600
              dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" placeholder="gravatar email address" />
            {ValidationErrors.renderValidationErrors
               state.ValidationErrors
               "Gravatar email address"
               (state.Request.GravatarEmailAddress |> Option.defaultValue "")}
          </div>
          <button @click={Ev(fun _ -> dispatch Submit)} class="w-full text-white bg-blue-700 hover:bg-blue-800 focus:ring-4
            focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-5
            py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Edit
            profile</button>
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
