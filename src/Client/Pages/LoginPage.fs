module LoginPage

open System
open Elmish
open Fable.Core
open Feliz
open Shared

type State = {
  Username: string
  Password: string
  Error: string
}

type ExternalMsg = UserLoggedIn of UserModel

type Msg =
  | SetUsername of string
  | SetPassword of string
  | Submit
  | GotLoginResponse of Result<UserModel, LoginError>
  | ExternalMsg of ExternalMsg

let init () =
  {
    Username = ""
    Password = ""
    Error = ""
  },
  Cmd.none

let update (msg: Msg) (state: State) =
  match msg with
  | SetUsername username -> { state with Username = username }, Cmd.none
  | SetPassword password -> { state with Password = password }, Cmd.none
  | Submit ->
    state,
    Cmd.OfAsync.perform
      Remoting.serverApi.Login
      {
        Username = state.Username
        Password = state.Password
      }
      GotLoginResponse
  | GotLoginResponse(Ok user) -> state, Cmd.ofMsg (user |> UserLoggedIn |> ExternalMsg)
  | GotLoginResponse(Error loginError) ->
    match loginError with
    | InvalidUsernameAndOrPassword -> { state with Error = "Invalid username and/or password." }, Cmd.none
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
             prop.placeholder "password"
             prop.type' "password"
             prop.className "border border-black"
             prop.onTextChange (SetPassword >> dispatch)
           ]}
      <button onClick={fun _ -> dispatch Submit}>Login</button>
    </div>
    """
