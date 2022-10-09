module Domain

open System

type User = { Id: int; Username: string }

type VoteType =
    | Positive
    | Negative

type Vote =
    { PostId: int
      UserId: int
      Type: VoteType }

// type Poster =
//     | User of int
//     | Robot

type Post =
    { Id: int
      Headline: string
      Link: string
      CreatedAt: DateTime
      UpdatedAt: DateTime
      Votes: Vote list
      Poster: int option }

type Comment =
    { Id: int
      PostId: int
      UserId: int
      Parent: int option }

type CommentVote =
    { CommentId: int
      UserId: int
      Type: VoteType }
