module Persistence

open System
open System.Data
open Dapper.FSharp
open Dapper.FSharp.PostgreSQL
open Domain

type DbConnectionFactory = unit -> IDbConnection

let usersTable = table'<User> "Users"
let postsTable = table'<Post> "Posts"
let commentsTable = table'<Comment> "Comments"
let postVotesTable = table'<PostVote> "PostVotes"
let commentVotesTable = table'<CommentVote> "CommentVotes"

let insertPostsAsync (posts: Post list) (connection: IDbConnection) =
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

let getPostsWithVotesAsync (connection: IDbConnection) =
    select {
        for post in postsTable do
            leftJoin vote in postVotesTable on (post.Id = vote.PostId)
            leftJoin user in usersTable on (post.PosterId = Some user.Id)
            orderByDescending post.PublishedDate
            selectAll
    }
    |> connection.SelectAsyncOption<Post, PostVote, User>
