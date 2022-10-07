module Domain

open System

type User = { Id: int; Username: string }

type Poster =
    | User of int
    | Robot

type Post =
    { Id: int
      Headline: string
      Link: string
      Poster: Poster
      CreatedAt: DateTime
      UpdatedAt: DateTime }

type VoteType =
    | Positive
    | Negative

type Vote =
    { PostId: int
      UserId: int
      Type: VoteType }

type Comment =
    { Id: Guid
      PostId: int
      UserId: int
      Parent: Guid option }

type CommentVote =
    { CommentId: Guid
      UserId: int
      Type: VoteType }
