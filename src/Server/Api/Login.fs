module Login

open System.Threading.Tasks
open Data
open FsToolkit.ErrorHandling
open Shared
open DependencyTypes

type LoginService = LoginRequest -> Async<Result<UserModel, LoginError>>

let loginService
  (findUserByName: FindUserByName)
  (verifyPasswordHash: VerifyPasswordHash)
  (signInUser: SignInUser)
  : LoginService =
  fun loginRequest -> asyncResult {
    let! user =
      findUserByName loginRequest.Username
      |> AsyncResult.requireSome InvalidUsernameAndOrPassword

    do!
      verifyPasswordHash loginRequest.Password user
      |> Result.requireTrue InvalidUsernameAndOrPassword

    do! signInUser user
    return User.toSharedModel user
  }
