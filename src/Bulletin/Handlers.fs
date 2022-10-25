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

let scribanViewHandler (view: string) (model: 'a) : HttpHandler =
    withService<IViewEngine> (fun viewEngine -> Response.renderViewEngine viewEngine view model)

let googleOAuthHandler: HttpHandler =
    let authenticationProperties = AuthenticationProperties(RedirectUri = "/")
    Auth.challenge GoogleDefaults.AuthenticationScheme authenticationProperties

let postsHandler (querySession: IQuerySession) : HttpHandler =
    fun ctx ->
        task {
            let routeValues = Request.getRoute ctx
            let queryParams = Request.getQuery ctx

            let ordering =
                match routeValues.TryGetString("ordering") with
                | Some ordering when String.Equals("top", ordering, StringComparison.OrdinalIgnoreCase) -> TopScore
                | _ -> Latest

            let criteria =
                { Ordering = ordering
                  SearchQuery = queryParams.TryGetString("search")
                  Page = queryParams.GetInt("page", 1)
                  PageSize = Math.Min(50, queryParams.GetInt("pageSize", 50)) }

            let postModels (paginatedResults: Paginated<Post * User option>) =
                paginatedResults.Items
                |> Seq.map (fun (post, author) ->
                    {| headline = post.Headline
                       link = post.Link
                       score = post.Score
                       upvoted = false
                       downvoted = false
                       author =
                        author
                        |> Option.map (fun user -> user.Username)
                        |> Option.defaultValue "automated bot, probably." |})

            let! queryResults = getPostsAsync criteria querySession
            let models = postModels queryResults

            let responseModel =
                {| posts = models
                   current_page = queryResults.CurrentPage
                   has_next_page = queryResults.HasNextPage
                   has_previous_page = queryResults.HasPreviousPage
                   pages = queryResults.PageCount |}

            do! scribanViewHandler "index" responseModel ctx
        }
