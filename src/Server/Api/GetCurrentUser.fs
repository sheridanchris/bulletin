module GetCurrentUser

open Data
open Shared
open DependencyTypes

let getCurrentUser
  (getCurrentUserId: GetCurrentUserId)
  (findUserById: FindUserById)
  : GetCurrentUserService =
  fun () -> async {
    let currentUserId = getCurrentUserId ()

    match currentUserId with
    | None -> return Anonymous
    | Some id ->
      let! currentUser = findUserById id

      return
        currentUser
        |> Option.map (fun user -> User(User.toSharedModel user))
        |> Option.defaultValue Anonymous
  }
