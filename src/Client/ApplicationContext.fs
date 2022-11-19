module ApplicationContext

open Shared
open ElmishStore

type Model = { User: CurrentUser }

let init () = { User = Anonymous }, Cmd.none

type Msg =
  | LoggedIn of UserModel
  | LoggedOut

let update (msg: Msg) (model: Model) =
  match msg with
  | LoggedIn user -> { model with User = User user }, Cmd.none
  | LoggedOut -> { model with User = Anonymous }, Cmd.none

let dispose _ = ()
let store, dispatch = Store.makeElmish init update dispose ()
