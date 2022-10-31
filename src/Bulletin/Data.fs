module Data

open System

type User =
    { Id: Guid
      Username: string }

type VoteType =
    | Positive = 1
    | Negative = -1

type Vote =
    { Id: Guid
      VoteType: VoteType
      VoterId: Guid }

type Comment =
    { Id: Guid
      Text: string
      AuthorId: Guid
      Subcomments: Comment list
      Votes: Vote list }

type Post =
    { Id: Guid
      Headline: string
      Published: DateTime
      Link: string
      AuthorId: Guid Nullable
      Votes: Vote list
      Score: int // is this required?
      Comments: Comment list }
