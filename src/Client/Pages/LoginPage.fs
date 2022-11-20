module LoginPage

open System
open Elmish
open Fable.Core
open Lit
open Lit.Elmish
open LitStore
open LitRouter
open Shared

type State = {
  Username: string
  Password: string
  Error: string
}

type Msg =
  | SetUsername of string
  | SetPassword of string
  | Submit
  | GotLoginResponse of Result<UserModel, LoginError>

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
      Remoting.unsecuredServerApi.Login
      {
        Username = state.Username
        Password = state.Password
      }
      GotLoginResponse
  | GotLoginResponse(Ok user) ->
    state,
    Cmd.batch [
      Cmd.ofSub (fun _ -> ApplicationContext.dispatch (ApplicationContext.LoggedIn user))
      Cmd.navigate "/"
    ]
  | GotLoginResponse(Error loginError) ->
    match loginError with
    | InvalidUsernameAndOrPassword -> { state with Error = "Invalid username and/or password." }, Cmd.none

[<HookComponent>]
let Component () =
  let state, dispatch = Hook.useElmish (init, update)

  let renderError errorMsg =
    html
      $"""<p class="text-red-500">{errorMsg}</p>"""

  html
    $"""
    <div class="flex flex-col gap-y-3 justify-center items-center h-screen">
      {if String.IsNullOrWhiteSpace state.Error then
         Lit.nothing
       else
         renderError state.Error}

      <input placeholder="username" class="border border-black" @change={EvVal(SetUsername >> dispatch)} />
      <input placeholder="password" type="password" class="border border-black" @change={EvVal(SetPassword >> dispatch)} />
      <button @click={Ev(fun _ -> dispatch Submit)}>Login</button>
      <a href="/#/register">Don't have an account?</a>
    </div>
    """
