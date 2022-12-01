module CreateAccount

open System
open Shared
open Data
open FsToolkit.ErrorHandling
open FSharp.UMX
open DependencyTypes

let createAccountService
  (findUserByName: FindUserByName)
  (findUserByEmailAddress: FindUserByEmailAddress)
  (createPasswordHash: CreatePasswordHash)
  (signInUser: SignInUser)
  (saveUser: SaveUser)
  (createGravatarUrl: CreateGravatarUrl)
  : CreateAccountService =
  fun createAccountRequest -> asyncResult {
    do!
      findUserByName createAccountRequest.Username
      |> AsyncResult.requireNone UsernameTaken

    do!
      findUserByEmailAddress createAccountRequest.EmailAddress
      |> AsyncResult.requireNone EmailAddressTaken

    let user =
      User.create
        createAccountRequest.Username
        createAccountRequest.EmailAddress
        (createPasswordHash createAccountRequest.Password)
        (createGravatarUrl createAccountRequest.EmailAddress)

    do! saveUser user
    do! signInUser user
    return User.toSharedModel user
  }
