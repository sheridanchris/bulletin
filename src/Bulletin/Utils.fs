[<AutoOpen>]
module Utils

open System
open System.Collections.Generic
open FsToolkit.ErrorHandling
open Humanizer

module String =
    let equalsIgnoreCase (first: string) (second: string) =
        String.Equals(first, second, StringComparison.OrdinalIgnoreCase)

module DateTime =
    let friendlyDifference (before: DateTime) (after: DateTime) =
        let difference = after - before
        difference.Humanize()
