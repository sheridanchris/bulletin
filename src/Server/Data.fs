module Data

open System

type NewsSource =
  { Id: Guid
    ShortName: string
    RssFeed: string }

type User = { Id: string }

type VoteType =
  | Positive = 1
  | Negative = -1

type PostVote =
  { Id: Guid
    VoteType: VoteType
    PostId: Guid
    VoterId: string }

type Post =
  { Id: Guid
    Headline: string
    Published: DateTime
    Link: string
    AuthorName: string option
    Score: int
    FeedName: string }
