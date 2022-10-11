module Handlers

open Falco
open Falco.Middleware
open ScribanEngine
open Domain

let scribanViewHandler (view: string) (model: 'a) : HttpHandler =
    withService<IViewEngine> (fun viewEngine -> Response.renderViewEngine viewEngine view model)

let postsHandler : HttpHandler =
    fun ctx ->
        task {
            let! posts = Persistence.getPosts () |> Persistence.runAsync

            let getScore (post: Post) =
                post.Votes
                |> List.map (fun vote -> vote.Type) 
                |> List.sumBy (
                    function
                    | VoteType.Positive -> 1
                    | VoteType.Negative -> -1
                    | _ -> 0)
                
            let toModel (post: Post) =
                {| headline = post.Headline
                   score = getScore post
                   author = "" // todo
                   upvoted = false // todo
                   downvoted = false |} // todo

            let model = posts |> List.map toModel
            do! scribanViewHandler "index" model ctx
        }