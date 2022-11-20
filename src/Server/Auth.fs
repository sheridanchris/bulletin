module Auth

open System
open System.Security.Claims
open Data
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http

let private scheme = CookieAuthenticationDefaults.AuthenticationScheme
let private claims (user: User) = [| Claim(ClaimTypes.NameIdentifier, string user.Id) |]
let private claimsIdentity claims = ClaimsIdentity(claims, scheme)

let defaultProperties =
  let issued = DateTimeOffset.UtcNow
  let expires = issued.AddDays(6)
  AuthenticationProperties(IsPersistent = true, IssuedUtc = issued, ExpiresUtc = expires, AllowRefresh = true)

let signInWithProperties (properties: AuthenticationProperties) (context: HttpContext) (user: User) =
  let claimsPrincipal = user |> claims |> claimsIdentity |> ClaimsPrincipal
  context.SignInAsync(claimsPrincipal, properties)
