[<AutoOpen>]
module TestUtils

open System
open Data
open DependencyTypes
open Expecto
open FSharp.UMX

module User =
  let emptyUser = User.create String.Empty String.Empty String.Empty String.Empty

module DependencyTypeMocks =
  let findUserByName (f: string -> User option) : FindUserByName =
    fun username -> async { return f username }

  let findUserByEmailAddress (f: string -> User option) : FindUserByEmailAddress =
    fun emailAddress -> async { return f emailAddress }

  let verifyPasswordHash (result: bool) : VerifyPasswordHash = fun _ _ -> result

  let createPasswordHash (result: string) : CreatePasswordHash = fun _ -> result

  let signInUser: SignInUser = fun _ -> async { return () }

  let saveUser: SaveUser = fun _ -> async { return () }

  let createGravatarUrl (result: string) : CreateGravatarUrl = fun _ -> result

module Expect =
  let isOkWithPredicate (f: 'value -> bool) (result: Result<'value, _>) (message: string) =
    match result with
    | Ok value -> Expect.isTrue (f value) message
    | Error _ -> failtest message

  let isErrorWithPredicate (f: 'error -> bool) (result: Result<_, 'error>) (message: string) =
    match result with
    | Ok _ -> failtest message
    | Error value -> Expect.isTrue (f value) message
