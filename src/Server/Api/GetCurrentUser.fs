module GetCurrentUser

open Shared
open DependencyTypes

type CurrentUserService = unit -> Async<CurrentUser>

let getCurrentUser
  (getCurrentUserId: GetCurrentUserId)
  (findUserById: FindUserById)
  (userToSharedModel: UserToSharedModel)
  : CurrentUserService =
  fun () -> async {
    let currentUserId = getCurrentUserId ()

    match currentUserId with
    | None -> return Anonymous
    | Some id ->
      let! currentUser = findUserById id

      return
        currentUser
        |> Option.map (fun user -> User(userToSharedModel user))
        |> Option.defaultValue Anonymous
  }
