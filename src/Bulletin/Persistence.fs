module Persistence

open System
open System.Data
open Dapper.FSharp
open Dapper.FSharp.PostgreSQL
open Domain
open FsToolkit.ErrorHandling

type DbConnectionFactory = unit -> IDbConnection

type UserView = { Id: Guid; Username: string }

type PostView =
    { Id: Guid
      Headline: string
      Link: string
      PosterId: Guid option
      PublishedDate: DateTime }

type CommentView =
    { Id: Guid
      PostId: int
      UserId: int
      Comment: string
      ParentId: Guid option }

type PostVoteView =
    { PostId: Guid
      VoterId: Guid
      Type: VoteType }

type CommentVoteView =
    { CommentId: Guid
      VoterId: Guid
      Type: VoteType }

let usersTable = table'<UserView> "Users"
let postsTable = table'<PostView> "Posts"
let commentsTable = table'<CommentView> "Comments"
let postVotesTable = table'<PostVoteView> "PostVotes"
let commentVotesTable = table'<CommentVoteView> "CommentVotes"

let toDomainPost (posts: seq<PostView * PostVoteView option * UserView option>) =
    posts
    |> Seq.groupBy fst3
    |> Seq.map (fun (post, values) ->
        let votes =
            values
            |> Seq.choose snd3
            |> Seq.map (fun voteView ->
                { VoterId = UserId voteView.VoterId
                  VoteType = voteView.Type })
            |> Seq.toList

        let author =
            values
            |> Seq.map third
            |> Seq.tryFind Option.isSome
            |> Option.flatten
            |> Option.map (fun userView ->
                { UserId = UserId userView.Id
                  Username = userView.Username })

        { Id = PostId post.Id
          Headline = post.Headline
          Link = post.Link
          Author = author
          PublishedDate = post.PublishedDate
          Votes = votes })

let insertPostsAsync (posts: PostView list) (connection: IDbConnection) =
    insert {
        into postsTable
        values posts
    }
    |> connection.InsertAsync

// NOTE: This exists for comparison checks.
// While polling the RSS feed, ill compare the latest post with the current stories published date.
let getLatestPostAsync (connection: IDbConnection) =
    select {
        for post in postsTable do
            orderByDescending post.PublishedDate
            take 1
    }
    |> connection.SelectAsync<{| PublishedDate: DateTime |}>

let getPostsAsync (connection: IDbConnection) =
    select {
        for post in postsTable do
            leftJoin vote in postVotesTable on (post.Id = vote.PostId)
            leftJoin user in usersTable on (post.PosterId = Some user.Id)
            orderByDescending post.PublishedDate
    }
    |> connection.SelectAsyncOption<PostView, PostVoteView, UserView>
    |> Task.map toDomainPost

let searchPostsAsync (searchParam: string) (connection: IDbConnection) =
    select {
        for post in postsTable do
            leftJoin vote in postVotesTable on (post.Id = vote.PostId)
            leftJoin user in usersTable on (post.PosterId = Some user.Id)
            where (like post.Headline searchParam)
            orderByDescending post.PublishedDate
    }
    |> connection.SelectAsyncOption<PostView, PostVoteView, UserView>
    |> Task.map toDomainPost
