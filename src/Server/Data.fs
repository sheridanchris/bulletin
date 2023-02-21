module Data

open System
open FSharp.UMX
open Shared

type RssFeed = { Id: FeedId; RssFeedUrl: string }

type User = {
  Id: UserId
  Username: string
  EmailAddress: string
  GravatarEmailAddress: string
  PasswordHash: string
  ProfilePictureUrl: string
}

type Category = {
  Id: CategoryId
  UserId: UserId
  Name: string
}

type FeedSubscription = {
  Id: SubscriptionId
  UserId: UserId
  Category: Nullable<CategoryId>
  FeedId: FeedId
  FeedName: string
}

type Post = {
  Id: PostId
  Headline: string
  PublishedAt: DateTime
  LastUpdatedAt: DateTime
  Link: string
  Feed: FeedId
}

module User =
  let create username emailAddress passwordHash profilePictureUrl = {
    Id = % Guid.NewGuid()
    Username = username
    EmailAddress = emailAddress
    GravatarEmailAddress = emailAddress
    PasswordHash = passwordHash
    ProfilePictureUrl = profilePictureUrl
  }

  let toSharedModel (user: User) : UserModel = {
    Id = user.Id
    Username = user.Username
    EmailAddress = user.EmailAddress
    GravatarEmailAddress = user.GravatarEmailAddress
    ProfilePictureUrl = user.ProfilePictureUrl
  }
