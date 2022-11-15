[<AutoOpen>]
module Helpers

open Fable.Core
open Feliz
open Browser.Types

let inline toJsx (el: ReactElement) : JSX.Element = unbox el
let inline toReact (el: JSX.Element) : ReactElement = unbox el

/// Enables use of Feliz styles within a JSX hole
let inline toStyle (styles: IStyleAttribute list) : obj = JsInterop.createObj (unbox styles)
