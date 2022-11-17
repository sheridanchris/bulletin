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
  VoteType: VoteType // can only be 1 or -1 -- this is a primitive :(
  VoterId: Guid
}

type Post = {
  Id: Guid
  Headline: string
  Published: DateTime
  Link: string
  AuthorName: string option
  Votes: PostVote list
  Score: int
  FeedName: string
}
