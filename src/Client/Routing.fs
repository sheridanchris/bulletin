module Routing

type Page =
  | Home
  | Login
  | Register
  | Feed
  | Subscriptions
  | Profile
  | EditProfile
  | ChangePassword
  | NotFound

[<RequireQualifiedAccess>]
module Page =
  let parseUrl routes =
    match routes with
    | [] -> Home
    | [ "feed" ] -> Feed
    | [ "login" ] -> Login
    | [ "register" ] -> Register
    | [ "subscriptions" ] -> Subscriptions
    | [ "profile" ] -> Profile
    | [ "profile"; "edit" ] -> EditProfile
    | [ "profile"; "edit"; "password" ] -> ChangePassword
    | _ -> NotFound
