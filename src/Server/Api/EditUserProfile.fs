module EditUserProfile

open FsToolkit.ErrorHandling
open Data
open Shared
open DependencyTypes

let editUserProfileService
  (getCurrentUserId: GetCurrentUserId)
  (findUserById: FindUserById)
  (saveUser: SaveUser)
  (createGravatarUrl: CreateGravatarUrl)
  : EditUserProfileService =
  fun request -> asyncResult {
    let currentUserId = getCurrentUserId () |> Option.get

    let! user =
      findUserById currentUserId
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
            |> createGravatarUrl
      }

    do! saveUser updatedUser
    return User.toSharedModel updatedUser
  }
