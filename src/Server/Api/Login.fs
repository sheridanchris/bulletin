module Login

open System.Threading.Tasks
open Data
open FsToolkit.ErrorHandling
open Shared
open DataAccess
open DependencyTypes
open BCrypt.Net

let loginService (findUserAsync: FindUserAsync) (signInUser: SignInUser) : LoginService =
  fun loginRequest -> asyncResult {
    let! user =
      loginRequest.Username
      |> FindByUsername
      |> findUserAsync
      |> AsyncResult.requireSome InvalidUsernameAndOrPassword

    do!
      BCrypt.Verify(loginRequest.Password, user.PasswordHash)
      |> Result.requireTrue InvalidUsernameAndOrPassword

    do! signInUser user
    return User.toSharedModel user
  }
