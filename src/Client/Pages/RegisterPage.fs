module RegisterPage

open System
open Elmish
open Fable.Core
open Fable.Remoting.Client
open Feliz
open Shared

type State = {
  Username: string
  EmailAddress: string
  Password: string
  ConfirmPassword: string
  Error: string
}

type ExternalMsg = AccountCreated of UserModel

type Msg =
  | SetUsername of string
  | SetEmailAddress of string
  | SetPassword of string
  | SetConfirmPassword of string
  | Submit
  | GotResponse of Result<UserModel, CreateAccountError>
  | ExternalMsg of ExternalMsg

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
  | GotResponse(Ok userModel) -> state, Cmd.ofMsg (userModel |> AccountCreated |> ExternalMsg)
  | GotResponse(Error creationError) ->
    match creationError with
    | UsernameTaken -> { state with Error = "That username already exists" }, Cmd.none
    | EmailAddressTaken -> { state with Error = "That email address already exists" }, Cmd.none
  | ExternalMsg _ -> state, Cmd.none

let Component (state: State) (dispatch: Msg -> unit) =
  JSX.jsx
    $"""
    <div className="flex flex-col gap-y-3 justify-center items-center h-screen">
      {if String.IsNullOrWhiteSpace state.Error then
         Html.none
       else
         Html.p [
           prop.className "text-red-500"
           prop.text state.Error
         ]}
         {Html.input [
            prop.placeholder "username"
            prop.className "border border-black"
            prop.onTextChange (SetUsername >> dispatch)
          ]}
         {Html.input [
            prop.placeholder "email address"
            prop.className "border border-black"
            prop.onTextChange (SetEmailAddress >> dispatch)
          ]}
          {Html.input [
             prop.placeholder "password"
             prop.type' "password"
             prop.className "border border-black"
             prop.onTextChange (SetPassword >> dispatch)
           ]}
          {Html.input [
             prop.placeholder "confirm password"
             prop.type' "password"
             prop.className "border border-black"
             prop.onTextChange (SetConfirmPassword >> dispatch)
           ]}
      <button onClick={fun _ -> dispatch Submit}>Create Account</button>
    </div>
    """
