module App

open Fable.Core
open Lit
open LitRouter
open Fable.Remoting.Client
open Shared
open Fable.Core.JsInterop

importSideEffects "./index.css"

[<LitElement("my-app")>]
let MyApp () =

  let _ = LitElement.init (fun cfg -> cfg.useShadowDom <- false)
  let path = Hook.useRouter (RouteMode.Hash)

  match path with
  | [ "login" ] -> LoginPage.Component()
  | [ "register" ] -> RegisterPage.Component()
  | _ -> PostsPage.Component()
