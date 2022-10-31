module DataAccess

open System
open System.Collections.Generic
open System.Threading
open Marten
open Marten.Pagination
open FsToolkit.ErrorHandling
open Data

type PostOrdering =
    | Latest
    | TopScore

type PostCriteria =
    { Ordering: PostOrdering
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

let latestPostAsync (cancellationToken: CancellationToken) (querySession: IQuerySession) =
    querySession
    |> Session.query<Post>
    |> Queryable.orderByDescending <@ fun post -> post.Published @>
    |> Queryable.tryHeadTask cancellationToken
    |> TaskOption.map (fun post -> post.Published)

let getPostsAsync
    (criteria: PostCriteria)
    (cancellationToken: CancellationToken)
    (querySession: IQuerySession)
    =
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

        let orderPosts =
            match criteria.Ordering with
            | Latest -> Queryable.orderByDescending <@ fun post -> post.Published @>
            | TopScore -> Queryable.orderByDescending <@ fun post -> post.Score @>

        let filterPosts =
            match criteria.SearchQuery with
            | None -> id
            | Some query -> Queryable.filter <@ fun post -> post.Headline.PhraseSearch(query) @>

        let users = Dictionary<Guid, User>()

        return!
            querySession
            |> Session.query<Post>
            |> Queryable.includeDict <@ fun post -> post.AuthorId @> users
            |> filterPosts
            |> orderPosts
            |> Queryable.pagedListTask criteria.Page criteria.PageSize cancellationToken
            |> Task.map (join users)
    }
