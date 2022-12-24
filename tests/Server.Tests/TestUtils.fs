[<AutoOpen>]
module TestUtils

open System
open Expecto

module Expect =
  let isOkWithPredicate (f: 'value -> bool) (result: Result<'value, _>) (message: string) =
    match result with
    | Ok value -> Expect.isTrue (f value) message
    | Error _ -> failtest message

  let isErrorWithPredicate (f: 'error -> bool) (result: Result<_, 'error>) (message: string) =
    match result with
    | Ok _ -> failtest message
    | Error value -> Expect.isTrue (f value) message
