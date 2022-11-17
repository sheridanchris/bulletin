[<AutoOpen>]
module Utils

open System
open Humanizer

module String =
  let equalsIgnoreCase (first: string) (second: string) =
    String.Equals(first, second, StringComparison.OrdinalIgnoreCase)

module DateTime =
  let friendlyDifference (before: DateTime) (after: DateTime) =
    let difference = after - before
    difference.Humanize()

module List =
  let tryFindWithIndex (f: 'a -> 'bool) (list: 'a list) =
    let rec loop (f: 'a -> bool) (index: int) (list: 'a list) =
      match list with
      | [] -> None
      | x :: _ when f x -> Some(index, x)
      | _ :: xs -> loop f (index + 1) xs

    loop f 0 list
