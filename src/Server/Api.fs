module Api

open System
open System.Security.Claims
open System.Threading
open Data
open Marten
open Marten.Pagination
open Microsoft.AspNetCore.Http
open Shared
open FsToolkit.ErrorHandling

let toPaginated (mapping: 'a -> 'b) (pagedList: IPagedList<'a>) : Paginated<'b> = {
  Items = pagedList |> Seq.map mapping
  CurrentPage = int pagedList.PageNumber
  PageSize = int pagedList.PageSize
  PageCount = int pagedList.PageCount
  HasNextPage = pagedList.HasNextPage
  HasPreviousPage = pagedList.HasPreviousPage
}

let toPostModel (upvoted: bool) (downvoted: bool) (post: Post) : PostModel =
  let published = DateTime.friendlyDifference post.Published DateTime.UtcNow

  {
    Id = post.Id
    Title = post.Headline
    Link = post.Link
    PublishedAt = published
    Author = post.AuthorName
    Score = post.Score
    Upvoted = upvoted
    Downvoted = downvoted
    Source = post.FeedName
  }

let getPosts (querySession: IQuerySession) (context: HttpContext) (getPostsModel: GetPostsModel) = task {
  let! posts = querySession |> DataAccess.getPostsAsync getPostsModel CancellationToken.None

  let nameIdentifier =
    context.User.FindFirstValue(ClaimTypes.NameIdentifier) |> Option.ofNull

  match nameIdentifier with
  | None -> return toPaginated (toPostModel false false) posts
  | Some userId ->
    let ids = [| for post in posts -> post.Id |]
    let! votes = DataAccess.getPostVotesAsync ids userId CancellationToken.None querySession

    return
      posts
      |> toPaginated (fun post ->
        let vote = votes |> Seq.tryFind (fun vote -> vote.PostId = post.Id)

        let upvoted, downvoted =
          match vote with
          | None -> false, false
          | Some vote -> vote.VoteType = VoteType.Positive, vote.VoteType = VoteType.Negative

        toPostModel upvoted downvoted post)
}

let serverApi (context: HttpContext) : ServerApi =
  let querySession = context.GetService<IQuerySession>()
  let documentSession = context.GetService<IDocumentSession>()
  { GetPosts = fun model -> getPosts querySession context model |> Async.AwaitTask }
