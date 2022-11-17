module App

open Fable.Core
open Elmish
open Fable.Remoting.Client
open Feliz
open Shared

type Page =
  | Posts of PostsPage.State
  | Login of LoginPage.State
  | Register of RegisterPage.State

type State = {
  CurrentPage: Page
  User: CurrentUser
}

type Msg =
  | SetCurrentUser of CurrentUser
  | PostsMsg of PostsPage.Msg
  | LoginMsg of LoginPage.Msg
  | RegisterMsg of RegisterPage.Msg

let init () =
  let postsState, postsCmd = PostsPage.init ()

  {
    CurrentPage = Posts postsState
    User = Anonymous
  },
  Cmd.batch [
    Cmd.map PostsMsg postsCmd
    Cmd.OfAsync.perform Remoting.serverApi.GetCurrentUser () SetCurrentUser
  ]

let handlePostsMsg (msg: PostsPage.Msg) (state: State) =
  match msg, state.CurrentPage with
  | msg, Posts postsState ->
    match msg with
    | PostsPage.ExternalMsg PostsPage.NavigateToLogin ->
      let newState, newCmd = LoginPage.init ()
      { state with CurrentPage = Login newState }, Cmd.map LoginMsg newCmd
    | PostsPage.ExternalMsg PostsPage.NavigateToRegister ->
      let newState, newCmd = RegisterPage.init ()
      { state with CurrentPage = Register newState }, Cmd.map RegisterMsg newCmd
    | _ ->
      let newState, newCmd = PostsPage.update msg postsState
      { state with CurrentPage = Posts newState }, Cmd.map PostsMsg newCmd
  | _ -> state, Cmd.none

let handleLoginMsg (msg: LoginPage.Msg) (state: State) =
  match msg, state.CurrentPage with
  | msg, Login loginState ->
    match msg with
    | LoginPage.ExternalMsg (LoginPage.UserLoggedIn user) ->
      let postsState, postsCmd = PostsPage.init ()
      { state with User = User user; CurrentPage = Posts postsState }, Cmd.map PostsMsg postsCmd
    | msg ->
      let newState, newCmd = LoginPage.update msg loginState
      { state with CurrentPage = Login newState }, Cmd.map LoginMsg newCmd
  | _ -> state, Cmd.none

let handleRegisterMsg (msg: RegisterPage.Msg) (state: State) =
  match msg, state.CurrentPage with
  | msg, Register registerState ->
    match msg with
    | RegisterPage.ExternalMsg (RegisterPage.AccountCreated user) ->
      let postsState, postsCmd = PostsPage.init ()
      { state with User = User user; CurrentPage = Posts postsState }, Cmd.map PostsMsg postsCmd
    | msg ->
      let newState, newCmd = RegisterPage.update msg registerState
      { state with CurrentPage = Register newState }, Cmd.map RegisterMsg newCmd
  | _ ->
    state, Cmd.none

let update msg state =
  match msg with
  | SetCurrentUser user -> { state with User = user }, Cmd.none
  | PostsMsg msg -> handlePostsMsg msg state
  | LoginMsg msg -> handleLoginMsg msg state
  | RegisterMsg msg ->
    printfn "register msg"
    handleRegisterMsg msg state

[<JSX.Component>]
let Component state dispatch =
  match state.CurrentPage with
  | Posts postState -> PostsPage.Posts state.User postState (PostsMsg >> dispatch)
  | Login loginState -> LoginPage.Component loginState (LoginMsg >> dispatch)
  | Register registerState -> RegisterPage.Component registerState (RegisterMsg >> dispatch)
