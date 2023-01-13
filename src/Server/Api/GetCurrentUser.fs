module GetCurrentUser

open Data
open DataAccess
open Shared
open DependencyTypes

let getCurrentUser (getCurrentUserId: GetCurrentUserId) (findUserAsync: FindUserAsync) : GetCurrentUserService =
  fun () -> async {
    let currentUserId = getCurrentUserId ()

    match currentUserId with
    | None -> return Anonymous
    | Some id ->
      let! currentUser = FindById id |> findUserAsync

      return
        currentUser
        |> Option.map (fun user -> User(User.toSharedModel user))
        |> Option.defaultValue Anonymous
  }
