module ApplicationContext

open Shared
open ElmishStore

type Model = { User: CurrentUser }

type Msg =
  | LoggedIn of UserModel
  | LoggedOut
  | SetCurrentUser of CurrentUser

let init () =
  { User = Anonymous }, Cmd.OfAsync.perform Remoting.unsecuredServerApi.GetCurrentUser () SetCurrentUser

let update (msg: Msg) (model: Model) =
  match msg with
  | LoggedIn user -> { model with User = User user }, Cmd.none
  | LoggedOut -> { model with User = Anonymous }, Cmd.none
  | SetCurrentUser user -> { model with User = user }, Cmd.none

let dispose _ = ()
let store, dispatch = Store.makeElmish init update dispose ()
