module DataAccess

open System
open System.Collections.Generic
open System.Threading
open Marten
open Marten.Pagination
open FsToolkit.ErrorHandling
open Data
open System.Linq

type Ordering =
    | Top
    | Latest
    | Oldest

type PostCriteria =
    { Ordering: Ordering
      SearchQuery: string option
      Page: int
      PageSize: int }

type CommentsCriteria =
    { PostId: Guid
      Ordering: Ordering }

type Paginated<'a> =
    { Items: 'a seq
      CurrentPage: int64
      PageSize: int64
      PageCount: int64
      HasNextPage: bool
      HasPreviousPage: bool }

let getSourcesAsync (cancellationToken: CancellationToken) (querySession: IQuerySession) =
    querySession
    |> Session.query<NewsSource>
    |> Queryable.toListTask cancellationToken

let latestPostAsync (cancellationToken: CancellationToken) (querySession: IQuerySession) =
    querySession
    |> Session.query<Post>
    |> Queryable.orderByDescending <@ fun post -> post.Published @>
    |> Queryable.tryHeadTask cancellationToken
    |> TaskOption.map (fun post -> post.Published)

let findPostsByUrls
    (urls: ResizeArray<string>)
    (cancellationToken: CancellationToken)
    (querySession: IQuerySession)
    =
    querySession
    |> Session.query<Post>
    |> Queryable.filter <@ fun post -> urls.Contains(post.Link) @>
    |> Queryable.toListTask cancellationToken
    |> Task.map Seq.toList
    |> Task.map (List.map (fun post -> post.Link))

let getPostsAsync
    (criteria: PostCriteria)
    (cancellationToken: CancellationToken)
    (querySession: IQuerySession)
    =
    task {
        let createPaginatedPost (posts: IPagedList<Post>) =
            let author post =
                post.AuthorName |> Option.defaultValue "automated bot, probably."

            { Items = posts |> Seq.map (fun post -> post, author post)
              CurrentPage = posts.PageNumber
              PageSize = posts.PageSize
              PageCount = posts.PageCount
              HasNextPage = posts.HasNextPage
              HasPreviousPage = posts.HasPreviousPage }

        let orderPosts =
            match criteria.Ordering with
            | Top -> Queryable.orderByDescending <@ fun post -> post.Score @>
            | Latest -> Queryable.orderByDescending <@ fun post -> post.Published @>
            | Oldest -> Queryable.orderBy <@ fun post -> post.Published @>

        let filterPosts =
            match criteria.SearchQuery with
            | None -> id
            | Some query -> Queryable.filter <@ fun post -> post.Headline.PhraseSearch(query) @>

        return!
            querySession
            |> Session.query<Post>
            |> filterPosts
            |> orderPosts
            |> Queryable.pagedListTask criteria.Page criteria.PageSize cancellationToken
            |> Task.map createPaginatedPost
    }

let getCommentsAsync
    (criteria: CommentsCriteria)
    (cancellationToken: CancellationToken)
    (querySession: IQuerySession)
    =
    let orderComments: IQueryable<Comment> -> IOrderedQueryable<Comment> =
        match criteria.Ordering with
        | Top -> Queryable.orderByDescending <@ fun comment -> comment.Score @>
        | Latest -> Queryable.orderByDescending <@ fun comment -> comment.Published @>
        | Oldest -> Queryable.orderBy <@ fun comment -> comment.Published @>

    querySession
    |> Session.query<Comment>
    |> Queryable.filter <@ fun comment -> comment.PostId = criteria.PostId @>
    |> orderComments
    |> Queryable.pagedListTask 1 100 cancellationToken