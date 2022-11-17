[<AutoOpen>]
module Helpers

open Fable.Core
open Feliz
open Browser.Types

let inline toJsx (el: ReactElement) : JSX.Element = unbox el
let inline toReact (el: JSX.Element) : ReactElement = unbox el

/// Enables use of Feliz styles within a JSX hole
let inline toStyle (styles: IStyleAttribute list) : obj = JsInterop.createObj (unbox styles)

module List =
  let inline updateIf (predicate: 'a -> bool) (mapper: 'a -> 'a) (list: 'a list) =
    let mapper current =
      if predicate current then mapper current else current

    list |> List.map mapper
