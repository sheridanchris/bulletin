module EditProfilePage

open System
open Alerts
open Lit
open Lit.Elmish
open LitRouter
open LitStore
open Shared
open Validus
open ValidatedInput

type State = {
  Username: ValidationState<string> option
  EmailAddress: ValidationState<string> option
  GravatarEmailAddress: ValidationState<string> option
  Alert: Alert
}

type Msg =
  | SetUsername of string
  | SetEmailAddress of string
  | SetGravatarEmailAddress of string
  | Submit
  | GotResult of Result<UserModel, EditUserProfileError>

let init () =
  {
    Username = None
    EmailAddress = None
    GravatarEmailAddress = None
    Alert = NothingToWorryAbout
  },
  Elmish.Cmd.none

let private validationStateIfNotEmpty (validator: string -> ValidationResult<string>) (value: string) =
  if String.IsNullOrWhiteSpace value then
    None
  else
    Some(ValidationState.create validator value)

let update (msg: Msg) (state: State) =
  match msg with
  | SetUsername username ->
    { state with
        Username = validationStateIfNotEmpty (Validators.usernameValidator "Username") username
    },
    Elmish.Cmd.none
  | SetEmailAddress emailAddress ->
    { state with
        EmailAddress = validationStateIfNotEmpty (Validators.emailAddressValidator "Email address") emailAddress
    },
    Elmish.Cmd.none
  | SetGravatarEmailAddress emailAddress ->
    { state with
        GravatarEmailAddress =
          validationStateIfNotEmpty (Validators.emailAddressValidator "Gravatar email address") emailAddress
    },
    Elmish.Cmd.none
  | Submit ->
    let extract optionalValidationState =
      match optionalValidationState with
      | Some(Valid value) -> Some value
      | _ -> None

    // NOTE(sheridanchris): This will update the present values that are in a valid state
    // any empty or invalid values will be ignored.
    let username = extract state.Username
    let emailAddress = extract state.EmailAddress
    let gravatarEmailAddress = extract state.GravatarEmailAddress

    state,
    Elmish.Cmd.OfAsync.perform
      Remoting.securedServerApi.EditUserProfile
      {
        Username = username
        EmailAddress = emailAddress
        GravatarEmailAddress = gravatarEmailAddress
      }
      GotResult
  | GotResult(Ok userModel) ->
    state,
    Elmish.Cmd.batch [
      Elmish.Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.SetCurrentUser(User userModel)))
      Cmd.navigate "profile"
    ]
  | GotResult(Error _) ->
    let alert =
      Danger
        {
          Reason = "Failed to edit your profile."
        }

    { state with Alert = alert }, Elmish.Cmd.none

let renderUser (state: State) (dispatch: Msg -> unit) (user: UserModel) =
  let optionalErrorComponent (key: string) (validationState: ValidationState<'a> option) =
    match validationState with
    | None -> Lit.nothing
    | Some validationState -> ErrorComponent "text-sm text-red-500" key validationState

  html
    $"""
    <div class="min-h-screen flex flex-col items-center justify-center">
      {AlertComponent state.Alert}
      <div class="w-full max-w-sm p-4 bg-white border border-gray-200 rounded-lg shadow-md sm:p-6 md:p-8 dark:bg-gray-800 dark:border-gray-700">
        <div class="space-y-6" action="#">
          <h5 class="text-xl font-medium text-gray-900 dark:text-white">Edit your profile</h5>
          <div>
            <label for="username" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your username</label>
            <input .value={user.Username} @change={EvVal(SetUsername >> dispatch)} type="text" name="username" id="username" class="bg-gray-50
            border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500
            focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500
            dark:placeholder-gray-400 dark:text-white" placeholder="username" /> {optionalErrorComponent "Username" state.Username}
          </div>
          <div>
            <label for="email-address" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your email address</label>
            <input .value={user.EmailAddress} @change={EvVal(SetEmailAddress >> dispatch)} type="text" name="email-address" id="email-address" class="bg-gray-50
            border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500
            focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500
            dark:placeholder-gray-400 dark:text-white" placeholder="email address" />
            {optionalErrorComponent "Email address" state.EmailAddress}
          </div>
          <div>
            <label for="gravatar-email-address" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your gravatar email address</label>
            <input .value={user.GravatarEmailAddress} @change={EvVal(SetGravatarEmailAddress >> dispatch)} type="text" name="gravatar-email-address" id="gravatar-email-address" />
            class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg
            focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600
            dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" placeholder="gravatar email address" />
            {optionalErrorComponent "Gravatar email address" state.GravatarEmailAddress}
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
