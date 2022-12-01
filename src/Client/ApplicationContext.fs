module ApplicationContext

open Shared
open ElmishStore

type Model = { User: CurrentUser }

type Msg =
  | SetLoggedIn of UserModel
  | SetLoggedOut
  | SetCurrentUser of CurrentUser

let init () =
  { User = Anonymous }, Cmd.OfAsync.perform Remoting.unsecuredServerApi.GetCurrentUser () SetCurrentUser

let update (msg: Msg) (model: Model) =
  match msg with
  | SetLoggedIn user -> { model with User = User user }, Cmd.none
  | SetLoggedOut -> { model with User = Anonymous }, Cmd.none
  | SetCurrentUser user -> { model with User = user }, Cmd.none

let dispose _ = ()
let store, dispatch = Store.makeElmish init update dispose ()
