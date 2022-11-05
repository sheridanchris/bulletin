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

type Vote =
    { Id: Guid
      VoteType: VoteType
      VoterId: string }

type Comment =
    { Id: Guid
      Text: string
      Deleted: bool
      PostId: Guid
      AuthorId: string
      Published: DateTime
      Children: Comment list // TODO: Do I want to load them all at once?
      Score: int
      Votes: Vote list }

type Post =
    { Id: Guid
      Headline: string
      Published: DateTime
      Link: string

      // this is a workaround
      // this is a foreign key... BUT, optional foreign key's don't work with Marten
      // AND strings can't be `Nullable<_>` afaik
      AuthorName: string option

      Votes: Vote list
      Score: int } // is this required?
