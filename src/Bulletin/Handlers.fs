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

let postsHandler: HttpHandler =
    let handler (querySession: IQuerySession) : HttpHandler =
        fun ctx ->
            task {
                let routeValues = Request.getRoute ctx
                let queryParams = Request.getQuery ctx

                let ordering =
                    match routeValues.GetString("ordering", "") with
                    | ordering when String.Equals("top", ordering, StringComparison.OrdinalIgnoreCase) -> TopScore
                    | _ -> Latest

                let searchQuery =
                    match queryParams.GetString("search", "") with
                    | "" -> None
                    | value -> Some value

                let page = queryParams.GetInt("page", 1)
                let pageSize = Math.Min(50, queryParams.GetInt("pageSize", 50))

                let criteria =
                    { Ordering = ordering
                      SearchQuery = searchQuery
                      Page = page
                      PageSize = pageSize }

                let! queryResults = getPostsAsync criteria querySession

                let postModels =
                    queryResults
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

                do! scribanViewHandler "index" {| posts = postModels |} ctx
            }

    withService handler
