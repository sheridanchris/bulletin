module Data

open System
open Marten
open System.Collections.Generic
open FsToolkit.ErrorHandling
open System.Threading
open Marten.Pagination

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

type Paginated<'a> =
    { Items: 'a seq
      CurrentPage: int64
      PageSize: int64
      PageCount: int64
      HasNextPage: bool
      HasPreviousPage: bool }

let latestPostAsync (querySession: IQuerySession) =
    querySession
    |> Session.query<Post>
    |> Queryable.orderByDescending <@ fun post -> post.Published @>
    |> Queryable.tryHeadTask CancellationToken.None
    |> TaskOption.map (fun post -> post.Published)

let getPostsAsync (criteria: PostCriteria) (querySession: IQuerySession) =
    task {
        let join (users: Dictionary<Guid, User>) (posts: IPagedList<Post>) =
            let author post =
                post.AuthorId
                |> Option.ofNullable
                |> Option.map (fun authorId -> Dictionary.tryFindValue authorId users)
                |> Option.flatten

            { Items = posts |> Seq.map (fun post -> post, author post)
              CurrentPage = posts.PageNumber
              PageSize = posts.PageSize
              PageCount = posts.PageCount
              HasNextPage = posts.HasNextPage
              HasPreviousPage = posts.HasPreviousPage }

        let ordering queryable =
            match criteria.Ordering with
            | Latest -> Queryable.orderByDescending <@ fun post -> post.Published @> queryable
            | TopScore -> Queryable.orderByDescending <@ fun post -> post.Score @> queryable

        let filter queryable =
            match criteria.SearchQuery with
            | None -> queryable
            | Some query -> Queryable.filter <@ fun post -> post.Headline.PhraseSearch(query) @> queryable

        let users = Dictionary<Guid, User>()

        return!
            querySession
            |> Session.query<Post>
            |> Queryable.includeDict <@ fun post -> post.AuthorId @> users
            |> filter
            |> ordering
            |> Queryable.pagedListTask criteria.Page criteria.PageSize CancellationToken.None
            |> Task.map (join users)
    }
