module Data

open System
open Marten
open System.Collections.Generic
open FsToolkit.ErrorHandling

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
      AuthorId: Guid Nullable // TODO: Any way to use an option here?
      Votes: Vote list
      Score: int // is this required?
      Comments: Comment list }

type Ordering =
    | Latest
    | TopScore

type PostCriteria =
    { Ordering: Ordering
      SearchQuery: string option
      Page: int
      PageSize: int }

let latestPostAsync (querySession: IQuerySession) =
    querySession
    |> Session.query<Post>
    |> Queryable.orderByDescending <@ fun post -> post.Published @>
    |> Queryable.tryHeadTask
    |> TaskOption.map (fun post -> post.Published)

let getPostsAsync (criteria: PostCriteria) (querySession: IQuerySession) =
    task {
        let users = Dictionary<Guid, User>()

        let join (post: Post) =
            let author =
                post.AuthorId
                |> Option.ofNullable
                |> Option.map (fun authorId -> Dictionary.tryFindValue authorId users)
                |> Option.flatten

            post, author

        let ordering =
            match criteria.Ordering with
            | Latest -> <@ fun post -> post.Published.Millisecond @>
            | TopScore -> <@ fun post -> post.Score @>

        let filter =
            match criteria.SearchQuery with
            | None -> <@ fun _ -> true @>
            | Some query -> <@ fun post -> post.Headline.PhraseSearch(query) @>

        let skip = criteria.PageSize * criteria.Page - criteria.PageSize
        let take = criteria.PageSize

        return!
            querySession
            |> Session.query<Post>
            |> Queryable.includeDict <@ fun post -> post.AuthorId @> users
            |> Queryable.filter filter
            |> Queryable.orderByDescending ordering
            |> Queryable.paging skip take
            |> Queryable.toListAsync
            |> Async.map (Seq.map join)
    }
