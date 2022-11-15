open Fable.Core
open Browser
open Fable.React
open Fable.Core.JsInterop

importSideEffects "./index.css"
let root = ReactDomClient.createRoot (document.getElementById ("app-container"))
root.render (App.Component())
