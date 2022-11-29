module Data

open System
open FSharp.UMX
open Shared

[<Measure>]
type FeedId

[<Measure>]
type UserId

[<Measure>]
type PostId

[<Measure>]
type FeedSubscriptionId

type RssFeed = {
  Id: Guid<FeedId>
  RssFeedUrl: string
}

type User = {
  Id: Guid<UserId>
  Username: string
  EmailAddress: string
  GravatarEmailAddress: string
  PasswordHash: string
  ProfilePictureUrl: string
}

type FeedSubscription = {
  Id: Guid<FeedSubscriptionId>
  UserId: Guid<UserId>
  FeedId: Guid<FeedId>
  FeedName: string
}

type Post = {
  Id: Guid<PostId>
  Headline: string
  PublishedAt: DateTime
  LastUpdatedAt: DateTime
  Link: string
  Feed: Guid<FeedId>
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
    Id = %user.Id
    Username = user.Username
    EmailAddress = user.EmailAddress
    ProfilePictureUrl = user.ProfilePictureUrl
  }
