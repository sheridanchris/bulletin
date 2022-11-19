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
      Remoting.serverApi.CreateAccount
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
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  let renderError errorMsg =
    html
      $"""
      <p class="text-red-500">{errorMsg}</p>
      """

  html
    $"""
    <div class="flex flex-col gap-y-3 justify-center items-center h-screen">
      {if String.IsNullOrWhiteSpace state.Error then
         Lit.nothing
       else
         renderError state.Error}

      <input placeholder="username" class="border border-black" @change={EvVal(SetUsername >> dispatch)} />
      <input placeholder="email address" class="border border-black" @change={EvVal(SetEmailAddress >> dispatch)} />
      <input placeholder="password" type="password" class="border border-black" @change={EvVal(SetPassword >> dispatch)} />
      <input placeholder="confirm password" type="password" class="border border-black" @change={EvVal(SetConfirmPassword >> dispatch)} />
      <button @click={Ev(fun _ -> dispatch Submit)}>Register</button>
    </div>
    """
