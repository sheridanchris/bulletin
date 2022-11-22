module RegisterPage

open System
open Elmish
open Fable.Core
open Fable.Remoting.Client
open Shared
open Lit
open Lit.Elmish
open LitStore
open LitRouter

type State = {
  Username: string
  EmailAddress: string
  Password: string
  ConfirmPassword: string
  Error: string
}

type Msg =
  | SetUsername of string
  | SetEmailAddress of string
  | SetPassword of string
  | SetConfirmPassword of string
  | Submit
  | GotResponse of Result<UserModel, CreateAccountError>

let init () =
  {
    Username = ""
    EmailAddress = ""
    Password = ""
    ConfirmPassword = ""
    Error = ""
  },
  Cmd.none

let update (msg: Msg) (state: State) =
  match msg with
  | SetUsername username -> { state with Username = username }, Cmd.none
  | SetEmailAddress emailAddress -> { state with EmailAddress = emailAddress }, Cmd.none
  | SetPassword password ->
    let error =
      if password = state.ConfirmPassword then
        ""
      else
        "Passwords do not match."

    { state with
        Password = password
        Error = error
    },
    Cmd.none
  | SetConfirmPassword confirmPassword ->
    let error =
      if confirmPassword = state.Password then
        ""
      else
        "Passwords do not match."

    { state with
        ConfirmPassword = confirmPassword
        Error = error
    },
    Cmd.none
  | Submit ->
    state,
    Cmd.OfAsync.perform
      Remoting.unsecuredServerApi.CreateAccount
      {
        Username = state.Username
        EmailAddress = state.EmailAddress
        Password = state.Password
      }
      GotResponse
  | GotResponse(Ok userModel) ->
    state,
    Cmd.batch [
      Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.LoggedIn userModel))
      Cmd.navigate "/"
    ]
  | GotResponse(Error creationError) ->
    match creationError with
    | UsernameTaken -> { state with Error = "That username already exists" }, Cmd.none
    | EmailAddressTaken -> { state with Error = "That email address already exists" }, Cmd.none

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
        @change={EvVal(onChanged)} />
    </div>
    """

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  let renderError errorMsg =
    html $"""<p class="text-red-500">{errorMsg}</p>"""

  html
    $"""
    <div class="min-h-screen flex items-center justify-center">
      <div class="w-full max-w-sm p-4 bg-white border border-gray-200 rounded-lg shadow-md sm:p-6 md:p-8 dark:bg-gray-800 dark:border-gray-700">
        <div class="space-y-6" action="#">
          <h5 class="text-xl font-medium text-gray-900 dark:text-white">Create your account</h5>
          <div>
            <label for="username" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your username</label>
            <input @change={EvVal(SetUsername >> dispatch)} type="text" name="username" id="username" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" placeholder="username" required>
          </div>
          <div>
            <label for="email" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your email address</label>
            <input @change={EvVal(SetEmailAddress >> dispatch)} type="email" name="email" id="email" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" placeholder="username" required>
          </div>
          <div>
            <label for="password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Your password</label>
            <input @change={EvVal(SetPassword >> dispatch)} type="password" name="password" id="password" placeholder="••••••••" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" required>
          </div>
          <div>
            <label for="confirm-password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-white">Confirm your password</label>
            <input @change={EvVal(SetConfirmPassword >> dispatch)} type="password" name="confirm-password" id="confirm-password" placeholder="••••••••" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" required>
          </div>
          <button @click={Ev(fun _ -> dispatch Submit)} class="w-full text-white bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-5 py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Login</button>
          <div class="text-sm font-medium text-gray-500 dark:text-gray-300">
            Already have an account? <a href="/#/login" class="text-blue-700 hover:underline dark:text-blue-500">Login</a>
          </div>
        </div>
      </div>
    </div>
    """
