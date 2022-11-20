module CreateAccount

open System
open Shared
open Data
open FsToolkit.ErrorHandling
open FSharp.UMX
open DependencyTypes

type CreateAccountService = CreateAccountRequest -> Async<Result<UserModel, CreateAccountError>>

let createAccountService
  (findUserByName: FindUserByName)
  (findUserByEmailAddress: FindUserByEmailAddress)
  (createPasswordHash: CreatePasswordHash)
  (signInUser: SignInUser)
  (saveUser: SaveUser)
  (userToSharedModel: UserToSharedModel)
  : CreateAccountService =
  fun createAccountRequest -> asyncResult {
    do!
      findUserByName createAccountRequest.Username
      |> AsyncResult.requireNone UsernameTaken

    do!
      findUserByEmailAddress createAccountRequest.EmailAddress
      |> AsyncResult.requireNone EmailAddressTaken

    let userId = % Guid.NewGuid()
    let passwordHash = createPasswordHash createAccountRequest.Password

    let user = {
      Id = userId
      Username = createAccountRequest.Username
      EmailAddress = createAccountRequest.EmailAddress
      PasswordHash = passwordHash
    }

    do! saveUser user
    do! signInUser user
    return userToSharedModel user
  }