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
        | Some ordering when String.Equals("top", ordering, StringComparison.OrdinalIgnoreCase) ->
            TopScore
        | _ -> Latest

    let postModel (post, author) =
        let author =
            author
            |> Option.map (fun user -> user.Username)
            |> Option.defaultValue "automated bot, probably."

        {| headline = post.Headline
           link = post.Link
           score = post.Score
           upvoted = false
           downvoted = false
           author = author |}

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
