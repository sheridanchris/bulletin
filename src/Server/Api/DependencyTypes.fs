module DependencyTypes

open FSharp.UMX
open Data

type SignInUser = User -> Async<unit>
type GetCurrentUserId = unit -> Guid<UserId> option
