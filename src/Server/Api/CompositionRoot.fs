module CompositionRoot

open Microsoft.AspNetCore.Http
open Marten
open Shared
open DataAccess

let unsecuredServerApi (httpContext: HttpContext) : UnsecuredServerApi =
  let querySession = httpContext.GetService<IQuerySession>()
  let documentSession = httpContext.GetService<IDocumentSession>()

  {
    Login = Login.loginService (tryFindUserAsync querySession) (Authentication.signInUser httpContext)
    CreateAccount =
      CreateAccount.createAccountService
        (tryFindUserAsync querySession)
        (Authentication.signInUser httpContext)
        (saveAsync documentSession)
    GetCurrentUser =
      GetCurrentUser.getCurrentUser (Authentication.getCurrentUserId httpContext) (tryFindUserAsync querySession)
  }

let securedServerApi (httpContext: HttpContext) : SecuredServerApi =
  let querySession = httpContext.GetService<IQuerySession>()
  let documentSession = httpContext.GetService<IDocumentSession>()

  {
    GetSubscribedFeeds =
      GetSubscriptions.getSubscribedFeedsService
        (Authentication.getCurrentUserId httpContext)
        (getAllUserSubscriptionsWithFeeds querySession)
    GetUserFeed =
      GetUserFeed.getUserFeedService
        (Authentication.getCurrentUserId httpContext)
        (getAllUserSubscriptionsWithFeeds querySession)
        (getUserFeedAsync querySession)
    SubscribeToFeed =
      SubscribeToFeed.subscribeToFeedService
        (Authentication.getCurrentUserId httpContext)
        (getRssFeedByUrlAsync querySession)
        (getUserFeedSubscriptionAsync querySession)
        (saveAsync documentSession)
        (saveAsync documentSession)
    DeleteFeed =
      DeleteFeed.deleteFeedService
        (Authentication.getCurrentUserId httpContext)
        (getUserFeedSubscriptionAsync querySession)
        (deleteAsync documentSession)
    EditUserProfile =
      EditUserProfile.editUserProfileService
        (Authentication.getCurrentUserId httpContext)
        (tryFindUserAsync querySession)
        (saveAsync documentSession)
    ChangePassword =
      ChangePassword.changePasswordService
        (Authentication.getCurrentUserId httpContext)
        (tryFindUserAsync querySession)
        (saveAsync documentSession)
  }
