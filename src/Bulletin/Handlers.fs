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
    let ordering value =
        match value with
        | Some ordering when String.equalsIgnoreCase "top" ordering -> Top
        | Some ordering when String.equalsIgnoreCase "oldest" ordering -> Oldest
        | _ -> Latest

    fun ctx ->
        task {
            let routeValues = Request.getRoute ctx
            let queryParams = Request.getQuery ctx

            let pageQuery = queryParams.GetInt("page", 1)
            let pageSize = queryParams.GetInt("pageSize", 50)
            let searchQueryParam = queryParams.TryGetString("search")
            let ordering = routeValues.TryGetString("ordering") |> ordering

            let criteria =
                { Ordering = ordering
                  SearchQuery = searchQueryParam
                  Page = pageQuery
                  PageSize = Math.Min(50, pageSize) }

            let! queryResults =
                querySession
                |> getPostsAsync criteria CancellationToken.None
                |> Task.map Views.createPostModel

            do! scribanViewHandler "index" queryResults ctx
        }

let commentsHandler (querySession: IQuerySession) : HttpHandler =
    fun ctx ->
        task {
            let routeValues = Request.getRoute ctx
            let postId = routeValues.GetGuid("postId")

            let commentCriteria =
                { PostId = postId
                  Ordering = Latest }

            let! comments = getCommentsAsync commentCriteria CancellationToken.None querySession
            let comments = comments |> Seq.map Views.createCommentTree |> Seq.toList
            do! scribanViewHandler "comments" {| comments = comments |} ctx
        }
