module Handlers

open Falco
open Falco.Middleware
open ScribanEngine
open Domain
open Npgsql
open Persistence

let scribanViewHandler (view: string) (model: 'a) : HttpHandler =
    withService<IViewEngine> (fun viewEngine -> Response.renderViewEngine viewEngine view model)

let postsHandler : HttpHandler =
    let getScore (votes: PostVote option list) =
        votes
        |> List.filter Option.isSome
        |> List.map (fun vote -> vote.Value.Type) 
        |> List.sumBy (
            function
            | VoteType.Positive -> 1
            | VoteType.Negative -> -1
            | _ -> 0)
        
    let toModel (post: Post * PostVote option list) =
        let post, votes = post
        {| headline = post.Headline
           score = getScore votes
           upvoted = false // todo
           downvoted = false |} // todo

    let handler (dbConnectionFactory: DbConnectionFactory): HttpHandler =
        fun ctx ->
            task {
                use connection = dbConnectionFactory ()
                let! posts = connection |> getPostsWithVotesAsync

                let model =
                    posts
                    |> Seq.toList
                    |> List.groupBy fst
                    |> List.map (fun (key, value) -> key, value |> List.map snd)
                    |> List.map toModel

                do! scribanViewHandler "index" {| posts = model |} ctx
            }

    withService<DbConnectionFactory>(handler)