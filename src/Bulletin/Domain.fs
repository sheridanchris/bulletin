module Domain

open System

[<RequireQualifiedAccess>]
type RssVersion =
  | V1
  | V2

[<RequireQualifiedAccess>]
type FeedType =
  | Rss of RssVersion
  | Atom

type Feed = {
  Id: int
  Name: string
  Url: string
  Type: FeedType
}

type FeedEntry = {
  Id: int
  FeedId: int
  Title: string
  Url: string
  IsFavorited: bool
  Description: string option
  PublishedAt: DateTimeOffset
  UpdatedAt: DateTimeOffset
}
