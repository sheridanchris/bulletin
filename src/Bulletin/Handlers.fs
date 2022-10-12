module Handlers

open Falco
open Falco.Middleware
open ScribanEngine
open Domain
open Npgsql
open Persistence

let private fst3 (x, _, _) = x
let private snd3 (_, x, _) = x
let private third (_, _, x) = x

let scribanViewHandler (view: string) (model: 'a) : HttpHandler =
    withService<IViewEngine> (fun viewEngine -> Response.renderViewEngine viewEngine view model)

let postsHandler: HttpHandler =
    let getScore (votes: PostVote list) =
        votes
        |> List.sumBy (fun vote ->
            match vote.Type with
            | VoteType.Positive -> 1
            | VoteType.Negative -> -1
            | _ -> 0)

    let toModel (post: Post * PostVote list * User option) =
        let post, votes, author = post

        let author =
            author
            |> Option.map (fun user -> user.Username)
            |> Option.defaultValue "automated bot, probably."

        {| headline = post.Headline
           score = getScore votes
           author = author
           upvoted = false // todo
           downvoted = false |} // todo

    let handler (dbConnectionFactory: DbConnectionFactory) : HttpHandler =
        fun ctx ->
            task {
                use connection = dbConnectionFactory ()
                let! posts = connection |> getPostsWithVotesAsync

                let posts =
                    posts
                    |> Seq.toList
                    |> List.groupBy fst3
                    |> List.map (fun (post, values) ->
                        let votes =
                            values
                            |> List.map snd3
                            |> List.filter Option.isSome
                            |> List.map (fun x -> x.Value)

                        let author =
                            values |> List.map third |> List.tryFind Option.isSome |> Option.flatten

                        toModel (post, votes, author))

                do! scribanViewHandler "index" {| posts = posts |} ctx
            }

    withService<DbConnectionFactory> handler
