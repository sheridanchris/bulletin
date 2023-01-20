module CreateAccount

open System
open Giraffe
open Shared
open Data
open FsToolkit.ErrorHandling
open FSharp.UMX
open DataAccess
open BCrypt.Net
open Authentication

let createAccountService
  (findUserAsync: FindUserAsync)
  (signInUser: SignInUser)
  (saveUserAsync: SaveAsync<User>)
  : CreateAccountService =
  fun createAccountRequest -> asyncResult {
    do!
      createAccountRequest.Username
      |> FindByUsername
      |> findUserAsync
      |> AsyncResult.requireNone UsernameTaken

    do!
      createAccountRequest.EmailAddress
      |> FindByEmailAddress
      |> findUserAsync
      |> AsyncResult.requireNone EmailAddressTaken

    let passwordHash = BCrypt.HashPassword createAccountRequest.Password
    let profilePictureUrl = Gravatar.createUrl createAccountRequest.EmailAddress

    let user =
      User.create createAccountRequest.Username createAccountRequest.EmailAddress passwordHash profilePictureUrl

    do! saveUserAsync user
    do! signInUser user (defaultAuthenticationProperties ())

    return User.toSharedModel user
  }
