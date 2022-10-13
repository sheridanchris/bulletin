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

let postsHandler: HttpHandler =
    let getScore (votes: PostVote list) =
        votes
        |> List.sumBy (fun vote ->
            match vote.Type with
            | VoteType.Positive -> 1
            | VoteType.Negative -> -1
            | _ -> 0)

    let toModel currentUserId postInfo =
        let post, values = postInfo

        let votes =
            values
            |> List.map snd3
            |> List.filter Option.isSome
            |> List.map (fun x -> x.Value)

        let author =
            values
            |> List.map third
            |> List.tryFind Option.isSome
            |> Option.flatten
            |> Option.map (fun user -> user.Username)
            |> Option.defaultValue "automated bot, probably."

        let score = votes |> getScore

        let upvoted, downvoted =
            let postVoteType =
                currentUserId
                |> Option.bind (fun userId -> votes |> List.tryFind (fun vote -> vote.VoterId = userId))
                |> Option.map (fun postVote -> postVote.Type)

            match postVoteType with
            | Some VoteType.Positive -> true, false
            | Some VoteType.Negative -> false, true
            | Some _ | None -> false, false  

        {| headline = post.Headline
           score = score
           author = author
           upvoted = upvoted
           downvoted = downvoted |}

    let postModels currentUserId posts =
        posts |> Seq.toList |> List.groupBy fst3 |> List.map (toModel currentUserId)

    let handler (dbConnectionFactory: DbConnectionFactory) : HttpHandler =
        fun ctx ->
            task {
                use connection = dbConnectionFactory ()

                return!
                    connection
                    |> getPostsWithVotesAsync
                    |> Task.map (postModels None) // todo
                    |> Task.map (fun postModels -> scribanViewHandler "index" {| posts = postModels |} ctx)
            }

    withService<DbConnectionFactory> handler
