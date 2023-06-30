module ChangePassword

open FsToolkit.ErrorHandling
open Shared
open Authentication
open DataAccess
open Data
open BCrypt.Net

let changePasswordService
  (getCurrentUserId: GetCurrentUserId)
  (findUserAsync: FindUserAsync)
  (saveUserAsync: SaveAsync<User>)
  : ChangePasswordService =
  fun request ->
    asyncResult {
      Validation.failOnValidationErrors request.Validate

      let currentUserId = getCurrentUserId () |> Option.get

      let! user =
        FindById currentUserId
        |> findUserAsync
        |> AsyncResult.requireSome ChangePasswordError.UserNotFound

      do!
        BCrypt.Verify(request.CurrentPassword, user.PasswordHash)
        |> Result.requireTrue PasswordsDontMatch

      do!
        saveUserAsync {
          user with
              PasswordHash = BCrypt.HashPassword request.NewPassword
        }
    }
