[<AutoOpen>]
module Utils

open System.Collections.Generic
open FsToolkit.ErrorHandling

module Dictionary =
    let tryFindValue (key: 'key) (dict: Dictionary<'key, 'value>) =
        let _, value = dict.TryGetValue(key)
        value |> Option.ofNull
