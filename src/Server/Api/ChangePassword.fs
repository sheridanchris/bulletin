module ChangePassword

open FsToolkit.ErrorHandling
open Shared
open DependencyTypes

let changePasswordService
  (getCurrentUserId: GetCurrentUserId)
  (findUserById: FindUserById)
  (verifyPasswordHash: VerifyPasswordHash)
  (createPasswordHash: CreatePasswordHash)
  (saveUser: SaveUser)
  : ChangePasswordService =
  fun request -> asyncResult {
    let currentUserId = getCurrentUserId () |> Option.get

    let! user =
      findUserById currentUserId
      |> AsyncResult.requireSome ChangePasswordError.UserNotFound

    do!
      verifyPasswordHash request.CurrentPassword user
      |> Result.requireTrue PasswordsDontMatch

    let newPasswordHash = createPasswordHash request.NewPassword
    let newUser = { user with PasswordHash = newPasswordHash }

    do! saveUser newUser
  }
