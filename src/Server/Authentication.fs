module Authentication

open System
open System.Security.Claims
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open FSharp.UMX
open Data
open Shared

let private scheme = CookieAuthenticationDefaults.AuthenticationScheme
let private claims (user: User) = [| Claim(ClaimTypes.NameIdentifier, string user.Id) |]
let private claimsIdentity claims = ClaimsIdentity(claims, scheme)

let defaultAuthenticationProperties () =
  let issued = DateTimeOffset.UtcNow
  let expires = DateTimeOffset.UtcNow.AddDays(7)
  AuthenticationProperties(IsPersistent = true, IssuedUtc = issued, ExpiresUtc = expires, AllowRefresh = true)

let signInWithProperties (properties: AuthenticationProperties) (context: HttpContext) (user: User) =
  let claimsPrincipal = user |> claims |> claimsIdentity |> ClaimsPrincipal
  context.SignInAsync(claimsPrincipal, properties)

type SignInUser = User -> AuthenticationProperties -> Async<unit>
type GetCurrentUserId = unit -> UserId option

let signInUser (httpContext: HttpContext) : SignInUser =
  fun user properties ->
    let claimsPrincipal = user |> claims |> claimsIdentity |> ClaimsPrincipal
    httpContext.SignInAsync(claimsPrincipal, properties) |> Async.AwaitTask

let getCurrentUserId (httpContext: HttpContext) : GetCurrentUserId =
  fun () ->
    httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
    |> Option.ofNull
    |> Option.map (fun nameId -> % Guid.Parse(nameId.Value))
