module Domain

open System

type User = { Id: Guid; Username: string }

type Post =
    { Id: Guid
      Headline: string
      Tagline: string option
      Link: string
      PosterId: Guid option
      PublishedDate: DateTime } // when the article was published originally?? or on our site??

type Comment =
    { Id: Guid
      PostId: int
      UserId: int
      Comment: string
      ParentId: Guid option }

type VoteType =
    | Positive = 1
    | Negative = 2

type PostVote =
    { PostId: Guid
      VoterId: Guid
      Type: VoteType }

type CommentVote =
    { CommentId: Guid
      VoterId: Guid
      Type: VoteType }