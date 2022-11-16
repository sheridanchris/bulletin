module Data

open System

type NewsSource = {
  Id: Guid
  ShortName: string
  RssFeed: string
}

type User = {
  Id: Guid
  Username: string
  EmailAddress: string
  PasswordHash: string
}

type VoteType =
  | Positive = 1
  | Negative = -1

type PostVote = {
  Id: Guid
  VoteType: VoteType
  PostId: Guid
  VoterId: Guid
}

type Post = {
  Id: Guid
  Headline: string
  Published: DateTime
  Link: string
  AuthorName: string option
  Score: int
  FeedName: string
}
