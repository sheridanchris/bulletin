module CreateAccount

open System
open Shared
open Data
open FsToolkit.ErrorHandling
open FSharp.UMX
open DataAccess
open BCrypt.Net
open DependencyTypes

let createAccountService
  (findUserAsync: FindUserAsync)
  (signInUser: SignInUser)
  (saveUserAsync: SaveAsync<User>)
  (createGravatarUrl: CreateGravatarUrl)
  : CreateAccountService =
  fun createAccountRequest -> asyncResult {
    do!
      FindByUsername createAccountRequest.Username
      |> findUserAsync
      |> AsyncResult.requireNone UsernameTaken

    do!
      FindByEmailAddress createAccountRequest.EmailAddress
      |> findUserAsync
      |> AsyncResult.requireNone EmailAddressTaken

    let user =
      User.create
        createAccountRequest.Username
        createAccountRequest.EmailAddress
        (BCrypt.HashPassword createAccountRequest.Password)
        (createGravatarUrl createAccountRequest.EmailAddress)

    do! saveUserAsync user
    do! signInUser user

    return User.toSharedModel user
  }
