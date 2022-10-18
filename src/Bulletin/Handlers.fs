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

let private fst3 (x, _, _) = x
let private snd3 (_, x, _) = x
let private third (_, _, x) = x

let scribanViewHandler (view: string) (model: 'a) : HttpHandler =
    withService<IViewEngine> (fun viewEngine -> Response.renderViewEngine viewEngine view model)

let googleOAuthHandler: HttpHandler =
    let authenticationProperties = AuthenticationProperties(RedirectUri = "/")
    Auth.challenge GoogleDefaults.AuthenticationScheme authenticationProperties

let toModel currentUserId postInfo =
    let post, values = postInfo
    let votes: PostVote list = values |> List.choose snd3

    let author =
        values
        |> List.map third
        |> List.tryFind Option.isSome
        |> Option.flatten
        |> Option.map (fun user -> user.Username)
        |> Option.defaultValue "automated bot, probably."

    let postVoteType =
        currentUserId
        |> Option.bind (fun userId -> votes |> List.tryFind (fun vote -> vote.VoterId = userId))
        |> Option.map (fun postVote -> postVote.Type)

    let upvoted, downvoted =
        postVoteType = Some VoteType.Positive, postVoteType = Some VoteType.Negative

    {|
        headline = post.Headline
        link = post.Link
        score = post.Score
        author = author
        upvoted = upvoted
        downvoted = downvoted
    |}

let postModels currentUserId posts =
    posts |> Seq.toList |> List.groupBy fst3 |> List.map (toModel currentUserId)

let postsHandler: HttpHandler =
    let handler (dbConnectionFactory: DbConnectionFactory) : HttpHandler =
        fun ctx -> task {
            let queryReader = Request.getQuery ctx
            let searchQuery = queryReader.GetString("search", "")

            use connection = dbConnectionFactory ()

            let getPostsFunction =
                if String.IsNullOrWhiteSpace searchQuery
                then getPostsAsync
                else searchPostsAsync $"%%{searchQuery}%%" // free sql injection?

            return!
                connection
                |> getPostsFunction
                |> Task.map (postModels None) // todo
                |> Task.map (fun postModels -> scribanViewHandler "index" {| posts = postModels |} ctx)
        }

    withService<DbConnectionFactory> handler
