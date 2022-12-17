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

let private findUserById (querySession: IQuerySession) : FindUserById =
  fun userId -> querySession |> tryFindUserAsync (FindById userId)

let private findUserByName (querySession: IQuerySession) : FindUserByName =
  fun username -> querySession |> tryFindUserAsync (FindByUsername username)

let private findUserByEmailAddress (querySession: IQuerySession) : FindUserByEmailAddress =
  fun emailAddress -> querySession |> tryFindUserAsync (FindByEmailAddress emailAddress)

let private createPasswordHash: CreatePasswordHash =
  fun password -> BCrypt.HashPassword password

let private verifyPasswordHash: VerifyPasswordHash =
  fun password user -> BCrypt.Verify(password, user.PasswordHash)

let private saveUser (documentSession: IDocumentSession) : SaveUser =
  fun user -> documentSession |> saveUserAsync user

let private signInUser (httpContext: HttpContext) : SignInUser =
  fun user ->
    Auth.signInWithProperties Auth.defaultProperties httpContext user
    |> Async.AwaitTask

let private getCurrentUserId (httpContext: HttpContext) : GetCurrentUserId =
  fun () ->
    httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
    |> Option.ofNull
    |> Option.map (fun nameId -> % Guid.Parse(nameId.Value))

let private getRssFeedByUrl (querySession: IQuerySession) : GetRssFeedByUrl =
  fun feedUrl -> querySession |> getRssFeedByUrlAsync feedUrl

let private saveRssFeed (documentSession: IDocumentSession) : SaveRssFeed =
  fun rssFeed -> documentSession |> saveRssFeedAsync rssFeed

let private saveFeedSubscription (documentSession: IDocumentSession) : SaveFeedSubscription =
  fun feedSubscription -> documentSession |> saveFeedSubscriptionAsync feedSubscription

let private getFeedSubscription (querySession: IQuerySession) : GetFeedSubscription =
  fun userId feedId -> querySession |> getUserFeedSubscriptionAsync userId feedId

let private getSubscribedFeeds (querySession: IQuerySession) : GetSubscribedFeeds =
  fun userId -> querySession |> getAllUserSubscriptionsWithFeeds userId

let private getUserFeed (querySession: IQuerySession) : GetUserFeed =
  fun request feedIds -> querySession |> getUserFeedAsync request feedIds

let createGravatarUrl: CreateGravatarUrl =
  fun emailAddress ->
    use md5 = MD5.Create()

    let emailAddress = emailAddress.Trim().ToLowerInvariant()
    let emailBytes = Encoding.Default.GetBytes(emailAddress)
    let emailHash = md5.ComputeHash(emailBytes)

    let profilePictureHash =
      emailHash
      |> Seq.map (fun byte -> byte.ToString("x2"))
      |> String.concat ""

    $"https://www.gravatar.com/avatar/{profilePictureHash}"

let deleteSubscription (documentSession: IDocumentSession) : DeleteFeedSubscription =
  fun subscriptionId -> deleteFeedSubscription subscriptionId documentSession

let unsecuredServerApi (httpContext: HttpContext) : UnsecuredServerApi =
  let querySession = httpContext.GetService<IQuerySession>()
  let documentSession = httpContext.GetService<IDocumentSession>()

  {
    Login = Login.loginService (findUserByName querySession) verifyPasswordHash (signInUser httpContext)
    CreateAccount =
      CreateAccount.createAccountService
        (findUserByName querySession)
        (findUserByEmailAddress querySession)
        createPasswordHash
        (signInUser httpContext)
        (saveUser documentSession)
        createGravatarUrl
    GetCurrentUser = GetCurrentUser.getCurrentUser (getCurrentUserId httpContext) (findUserById querySession)
  }

let securedServerApi (httpContext: HttpContext) : SecuredServerApi =
  let querySession = httpContext.GetService<IQuerySession>()
  let documentSession = httpContext.GetService<IDocumentSession>()

  {
    GetSubscribedFeeds =
      GetSubscriptions.getSubscribedFeedsService (getCurrentUserId httpContext) (getSubscribedFeeds querySession)
    GetUserFeed =
      GetUserFeed.getUserFeedService
        (getCurrentUserId httpContext)
        (getSubscribedFeeds querySession)
        (getUserFeed querySession)
    SubscribeToFeed =
      SubscribeToFeed.subscribeToFeedService
        (getCurrentUserId httpContext)
        (getRssFeedByUrl querySession)
        (getFeedSubscription querySession)
        (saveRssFeed documentSession)
        (saveFeedSubscription documentSession)
    DeleteFeed =
      DeleteFeed.deleteFeedService
        (getCurrentUserId httpContext)
        (getFeedSubscription querySession)
        (deleteSubscription documentSession)
    EditUserProfile =
      EditUserProfile.editUserProfileService
        (getCurrentUserId httpContext)
        (findUserById querySession)
        (saveUser documentSession)
        createGravatarUrl
    ChangePassword =
      ChangePassword.changePasswordService
        (getCurrentUserId httpContext)
        (findUserById querySession)
        verifyPasswordHash
        createPasswordHash
        (saveUser documentSession)
  }
