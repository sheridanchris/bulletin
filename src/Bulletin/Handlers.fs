module Handlers

open System
open Falco
open Falco.Middleware
open ScribanEngine
open Domain
open Persistence
open Microsoft.AspNetCore.Authentication
open Falco.Security
open Microsoft.AspNetCore.Authentication.Google
open FsToolkit.ErrorHandling

let scribanViewHandler (view: string) (model: 'a) : HttpHandler =
    withService<IViewEngine> (fun viewEngine -> Response.renderViewEngine viewEngine view model)

let googleOAuthHandler: HttpHandler =
    let authenticationProperties = AuthenticationProperties(RedirectUri = "/")
    Auth.challenge GoogleDefaults.AuthenticationScheme authenticationProperties

let postsHandler: HttpHandler =
    let postModel currentUserId post =
        let userVote =
            currentUserId
            |> Option.map (fun userId -> Post.findUserVote userId post)
            |> Option.flatten
            |> Option.map (fun vote -> vote.VoteType)

        let upvoted = userVote = Some VoteType.Positive
        let downvoted = userVote = Some VoteType.Negative

        {|
            headline = post.Headline
            link = post.Link
            score = Post.calculateScore post
            author = Post.authorName post
            upvoted = upvoted
            downvoted = downvoted
        |}

    let handler (dbConnectionFactory: DbConnectionFactory) : HttpHandler =
        fun ctx -> task {
            let queryReader = Request.getQuery ctx
            let searchQuery = queryReader.GetString("search", "")

            use connection = dbConnectionFactory ()

            let getPostsFunction =
                if String.IsNullOrWhiteSpace searchQuery then
                    getPostsAsync
                else
                    searchPostsAsync $"%%{searchQuery}%%"

            return!
                connection
                |> getPostsFunction
                |> Task.map (Seq.map (postModel None)) // todo
                |> Task.map (fun postModels -> scribanViewHandler "index" {| posts = postModels |} ctx)
        }

    withService<DbConnectionFactory> handler
