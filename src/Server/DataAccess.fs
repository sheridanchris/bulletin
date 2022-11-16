module DataAccess

open System
open System.Collections.Generic
open System.Threading
open Marten
open Marten.Pagination
open FsToolkit.ErrorHandling
open Data
open System.Linq
open Shared

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

let findPostsByUrls (links: string[]) (cancellationToken: CancellationToken) (querySession: IQuerySession) =
  querySession
  |> Session.query<Post>
  |> Queryable.filter <@ fun post -> post.Link.IsOneOf(links) @>
  |> Queryable.toListTask cancellationToken

let getPostsAsync (criteria: GetPostsModel) (cancellationToken: CancellationToken) (querySession: IQuerySession) = task {
  let orderPosts (queryable: IQueryable<Post>) =
    match criteria.Ordering with
    | Top ->
      queryable
      |> Queryable.orderByDescending <@ fun post -> post.Score @>
      |> Queryable.orderByDescending <@ fun post -> post.Published @>
    | Latest -> queryable |> Queryable.orderByDescending <@ fun post -> post.Published @>
    | Oldest -> queryable |> Queryable.orderBy <@ fun post -> post.Published @>

  let filterPosts =
    match criteria.SearchQuery with
    | query when String.IsNullOrWhiteSpace query -> id
    | query -> Queryable.filter <@ fun post -> post.Headline.PhraseSearch(query) @>

  return!
    querySession
    |> Session.query<Post>
    |> filterPosts
    |> orderPosts
    |> Queryable.pagedListTask criteria.Page criteria.PageSize cancellationToken
}

let getPostVotesAsync
  (postIds: Guid[])
  (userId: Guid)
  (cancellationToken: CancellationToken)
  (querySession: IQuerySession)
  =
  querySession
  |> Session.query<PostVote>
  |> Queryable.filter <@ fun vote -> vote.PostId.IsOneOf(postIds) && vote.VoterId = userId @>
  |> Queryable.toListTask cancellationToken

type GetUserFilter =
  | FindById of Guid
  | FindByUsername of string
  | FindByEmailAddress of string

let tryFindUserAsync (filter: GetUserFilter) (querySession: IQuerySession) =
  let filter (queryable: IQueryable<User>) : IQueryable<User> =
    match filter with
    | FindById id -> queryable |> Queryable.filter <@ fun user -> user.Id = id @>
    | FindByUsername username -> queryable |> Queryable.filter <@ fun user -> user.Username = username @>
    | FindByEmailAddress email -> queryable |> Queryable.filter <@ fun user -> user.EmailAddress = email @>

  querySession
  |> Session.query<User>
  |> filter
  |> Queryable.tryHeadTask CancellationToken.None

let saveUserAsync (user: User) (documentSession: IDocumentSession) =
  documentSession |> Session.storeSingle user
  documentSession |> Session.saveChangesTask CancellationToken.None
