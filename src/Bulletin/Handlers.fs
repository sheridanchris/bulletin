module Handlers

open System
open Falco
open Falco.Middleware
open ScribanEngine
open Microsoft.AspNetCore.Authentication
open Falco.Security
open Microsoft.AspNetCore.Authentication.Google
open FsToolkit.ErrorHandling
open Marten
open Data
open DataAccess
open System.Threading
open Humanizer

type CommentTree =
    { id: Guid
      text: string
      score: int
      upvoted: bool
      downvoted: bool
      author: string
      depth: int
      published: string
      children: CommentTree list }

let scribanViewHandler (view: string) (model: 'a) : HttpHandler =
    withService<IViewEngine> (fun viewEngine -> Response.renderViewEngine viewEngine view model)

let googleOAuthHandler: HttpHandler =
    let authenticationProperties = AuthenticationProperties(RedirectUri = "/")
    Auth.challenge GoogleDefaults.AuthenticationScheme authenticationProperties

let postsHandler (querySession: IQuerySession) : HttpHandler =
    let createResponseModel posts paginatedResult =
        {| posts = posts
           current_page = paginatedResult.CurrentPage
           has_next_page = paginatedResult.HasNextPage
           has_previous_page = paginatedResult.HasPreviousPage
           pages = paginatedResult.PageCount |}

    let ordering value =
        match value with
        | Some ordering when String.equalsIgnoreCase "top" ordering -> Top
        | Some ordering when String.equalsIgnoreCase "oldest" ordering -> Oldest
        | _ -> Latest

    let postModel (post, author) =
        let published = DateTime.friendlyDifference post.Published DateTime.UtcNow

        {| id = post.Id
           headline = post.Headline
           link = post.Link
           score = post.Score
           upvoted = false // todo
           downvoted = false // todo
           author = author
           published = published |}

    fun ctx ->
        task {
            let routeValues = Request.getRoute ctx
            let queryParams = Request.getQuery ctx

            let pageQuery = queryParams.GetInt("page", 1)
            let pageSize = queryParams.GetInt("pageSize", 50)
            let searchQueryParam = queryParams.TryGetString("search")

            let orderingRouteValue = routeValues.TryGetString("ordering")
            let ordering = ordering orderingRouteValue

            let criteria =
                { Ordering = ordering
                  SearchQuery = searchQueryParam
                  Page = pageQuery
                  PageSize = Math.Min(50, pageSize) }

            let! queryResults = querySession |> getPostsAsync criteria CancellationToken.None

            let models = queryResults.Items |> Seq.map postModel
            let responseModel = createResponseModel models queryResults

            do! scribanViewHandler "index" responseModel ctx
        }

let commentModel (comment: Comment) =
    let rec commentModelRecursive (depth: int) (comment: Comment) =
        let published = DateTime.friendlyDifference comment.Published DateTime.UtcNow

        { id = comment.Id
          text = comment.Text
          score = comment.Score
          upvoted = false // todo
          downvoted = false // todo
          author = comment.AuthorId
          depth = depth
          published = published
          children = buildTree depth comment.Children }

    and buildTree (depth: int) (comments: Comment list) =
        match comments with
        | [] -> []
        | x :: xs ->
            let depth = depth + 1
            let model = commentModelRecursive depth x
            let res = (List.map (commentModelRecursive depth) xs)
            model :: res

    commentModelRecursive 1 comment

let commentsHandler (querySession: IQuerySession) : HttpHandler =

    fun ctx ->
        task {
            let routeValues = Request.getRoute ctx
            let postId = routeValues.GetGuid("postId")

            let commentCriteria =
                { PostId = postId
                  Ordering = Latest }

            let! comments = getCommentsAsync commentCriteria CancellationToken.None querySession
            let comments = comments |> Seq.map commentModel |> Seq.toList
            do! scribanViewHandler "comments" {| comments = comments |} ctx
        }
