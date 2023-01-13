module CompositionRoot

open System
open System.Security.Claims
open BCrypt.Net
open Data
open Marten
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open FSharp.UMX
open Shared
open DataAccess
open DependencyTypes
open System.Security.Cryptography
open System.Text

let private signInUser (httpContext: HttpContext) : SignInUser =
  fun user ->
    let expiry = TimeSpan.FromDays(6)
    let authenticationProperties = Auth.authenticationProperties expiry

    Auth.signInWithProperties authenticationProperties httpContext user
    |> Async.AwaitTask

let private getCurrentUserId (httpContext: HttpContext) : GetCurrentUserId =
  fun () ->
    httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
    |> Option.ofNull
    |> Option.map (fun nameId -> % Guid.Parse(nameId.Value))

let unsecuredServerApi (httpContext: HttpContext) : UnsecuredServerApi =
  let querySession = httpContext.GetService<IQuerySession>()
  let documentSession = httpContext.GetService<IDocumentSession>()

  {
    Login = Login.loginService (tryFindUserAsync querySession) (signInUser httpContext)
    CreateAccount =
      CreateAccount.createAccountService
        (tryFindUserAsync querySession)
        (signInUser httpContext)
        (saveAsync documentSession)
    GetCurrentUser = GetCurrentUser.getCurrentUser (getCurrentUserId httpContext) (tryFindUserAsync querySession)
  }

let securedServerApi (httpContext: HttpContext) : SecuredServerApi =
  let querySession = httpContext.GetService<IQuerySession>()
  let documentSession = httpContext.GetService<IDocumentSession>()

  {
    GetSubscribedFeeds =
      GetSubscriptions.getSubscribedFeedsService
        (getCurrentUserId httpContext)
        (getAllUserSubscriptionsWithFeeds querySession)
    GetUserFeed =
      GetUserFeed.getUserFeedService
        (getCurrentUserId httpContext)
        (getAllUserSubscriptionsWithFeeds querySession)
        (getUserFeedAsync querySession)
    SubscribeToFeed =
      SubscribeToFeed.subscribeToFeedService
        (getCurrentUserId httpContext)
        (getRssFeedByUrlAsync querySession)
        (getUserFeedSubscriptionAsync querySession)
        (saveAsync documentSession)
        (saveAsync documentSession)
    DeleteFeed =
      DeleteFeed.deleteFeedService
        (getCurrentUserId httpContext)
        (getUserFeedSubscriptionAsync querySession)
        (deleteAsync documentSession)
    EditUserProfile =
      EditUserProfile.editUserProfileService
        (getCurrentUserId httpContext)
        (tryFindUserAsync querySession)
        (saveAsync documentSession)
    ChangePassword =
      ChangePassword.changePasswordService
        (getCurrentUserId httpContext)
        (tryFindUserAsync querySession)
        (saveAsync documentSession)
  }
