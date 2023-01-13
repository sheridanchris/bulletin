module EditUserProfile

open FsToolkit.ErrorHandling
open Data
open DataAccess
open Shared
open DependencyTypes

let editUserProfileService
  (getCurrentUserId: GetCurrentUserId)
  (findUserAsync: FindUserAsync)
  (saveUserAsync: SaveAsync<User>)
  : EditUserProfileService =
  fun request -> asyncResult {
    let currentUserId = getCurrentUserId () |> Option.get

    let! user =
      FindById currentUserId
      |> findUserAsync
      |> AsyncResult.requireSome EditUserProfileError.UserNotFound

    // Since the request contains optional values, only update each property if the optional value is present
    // if not, use the existing value (don't update).
    let updatedUser =
      { user with
          Username = request.Username |> Option.defaultValue user.Username
          EmailAddress = request.EmailAddress |> Option.defaultValue user.EmailAddress
          GravatarEmailAddress = request.GravatarEmailAddress |> Option.defaultValue user.GravatarEmailAddress
          ProfilePictureUrl =
            request.GravatarEmailAddress
            |> Option.defaultValue user.GravatarEmailAddress
            |> Gravatar.createUrl
      }

    do! saveUserAsync updatedUser
    return User.toSharedModel updatedUser
  }
