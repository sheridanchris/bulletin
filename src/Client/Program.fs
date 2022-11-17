open Elmish
open Fable.Core
open Browser
open Fable.React
open Fable.Core.JsInterop
open Feliz

[<JSX.Component>]
let App () =
  let state, dispatch = React.useElmish (App.init, App.update)
  App.Component state dispatch

importSideEffects "./index.css"
let root = ReactDomClient.createRoot (document.getElementById "app-container")
root.render (App() |> toReact)
