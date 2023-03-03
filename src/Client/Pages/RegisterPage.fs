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
    CreateAccountRequest =
      {
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

  { state with
      CreateAccountRequest = createAccountRequest
      ValidationErrors = validationErrors
  }

let update (msg: Msg) (state: State) =
  match msg with
  | SetUsername username ->
    let request =
      { state.CreateAccountRequest with
          Username = username
      }

    let newState = updateCreateAccountRequest request state
    newState, Cmd.none
  | SetEmailAddress emailAddress ->
    let request =
      { state.CreateAccountRequest with
          EmailAddress = emailAddress
      }

    let newState = updateCreateAccountRequest request state
    newState, Cmd.none
  | SetPassword password ->
    let request =
      { state.CreateAccountRequest with
          Password = password
      }

    let newState = updateCreateAccountRequest request state
    newState, Cmd.none
  | SetConfirmPassword confirmPassword ->
    let request =
      { state.CreateAccountRequest with
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
      Alerts.Danger
        {
          Reason = "Something went wrong with that request!"
        }

    { state with Alert = Some alert }, Cmd.none

[<HookComponent>]
let FormInput (id: string) (labelValue: string) (inputType: string) (placeholder: string) (onChanged: string -> unit) =
  html
    $"""
    <div class="flex flex-col mb-5">
      <label for={id} class="mb-1 text-xs tracking-wide text-gray-600">
        {labelValue}
      </label>
      <input
        id={id}
        type={inputType}
        class="text-sm placeholder-gray-500 pl-3 pr-4 rounded-2xl border border-gray-400 py-2 focus:outline-none focus:border-blue-400"
        placeholder={placeholder}
        @keyup={EvVal(onChanged)} />
    </div>
    """

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  html
    $"""
    <div class="min-h-screen flex flex-col items-center justify-center">
      {Alerts.AlertComponent state.Alert}
      <div class="w-full max-w-sm p-4 bg-white border border-gray-200 rounded-lg shadow-md sm:p-6 md:p-8 dark:bg-gray-800 dark:border-gray-700">
        <div class="space-y-6" action="#">
          <h5 class="text-xl font-medium text-gray-900 dark:text-white">Create your account</h5>
          <div>
            <label for="username" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your username</label>
            <input @keyup={EvVal(SetUsername >> dispatch)} type="text" name="username" id="username" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" placeholder="username">
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Username" state.CreateAccountRequest.Username}
          </div>
          <div>
            <label for="email" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your email address</label>
            <input @keyup={EvVal(SetEmailAddress >> dispatch)} type="email" name="email" id="email" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" placeholder="email address">
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Email address" state.CreateAccountRequest.EmailAddress}
          </div>
          <div>
            <label for="password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your password</label>
            <input @keyup={EvVal(SetPassword >> dispatch)} type="password" name="password" id="password" placeholder="••••••••" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white">
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Password" state.CreateAccountRequest.Password}
          </div>
          <div>
            <label for="confirm-password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Confirm your password</label>
            <input @keyup={EvVal(SetConfirmPassword >> dispatch)} type="password" name="confirm-password" id="confirm-password" placeholder="••••••••" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white">
            {ValidationErrors.renderValidationErrors state.ValidationErrors "Confirmation" state.CreateAccountRequest.ConfirmPassword}
          </div>
          <button @click={Ev(fun _ -> dispatch Submit)} class="w-full text-white bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-5 py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Login</button>
          <div class="text-sm font-medium text-gray-500 dark:text-gray-300">
            Already have an account? <a href="/#/login" class="text-blue-700 hover:underline dark:text-blue-500">Login</a>
          </div>
        </div>
      </div>
    </div>
    """
