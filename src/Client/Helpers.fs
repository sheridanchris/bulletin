[<AutoOpen>]
module Helpers

module List =
  let inline updateIf (predicate: 'a -> bool) (mapper: 'a -> 'a) (list: 'a list) =
    list
    |> List.map (fun current -> if predicate current then mapper current else current)
