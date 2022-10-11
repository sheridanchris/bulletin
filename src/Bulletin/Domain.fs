module Domain

open System

type User = { Id: int; Username: string }

type VoteType =
    | Positive = 1
    | Negative = 2

type Vote =
    { PostId: int
      UserId: int
      Type: VoteType }

// type Poster =
//     | User of int
//     | Robot

type Comment =
    { Id: int
      PostId: int
      UserId: int
      ParentId: int option }

type CommentVote =
    { CommentId: int
      UserId: int
      Type: VoteType }

type Post =
    { Id: int
      Headline: string
      Description: string option
      Link: string
      Poster: User option // I don't think this will get loaded.
      PublishedDate: DateTimeOffset option
      //UpdatedAt: DateTime
      Votes: Vote list }
