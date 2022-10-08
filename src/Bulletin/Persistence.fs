module Persistence

open System.Data
open Microsoft.Data.Sqlite
open Donald
open Domain

let withConnection (f: IDbConnection -> 'a) =
    use connection = new SqliteConnection("Data Source=local.db")
    f connection

module Post =
    let toPoster intOption =
        match intOption with
        | Some id -> User id
        | None -> Robot

    let ofDataReader (dataReader: IDataReader) : Post =
        { Id = dataReader.ReadInt32 "id"
          Headline = dataReader.ReadString "headline"
          Link = dataReader.ReadString "link"
          Poster = dataReader.ReadInt32Option "poster_id" |> toPoster
          CreatedAt = dataReader.ReadDateTime "created_at"
          UpdatedAt = dataReader.ReadDateTime "updated_at" }

let getPostByIdAsync postId =
    withConnection (fun connection ->
        connection
        |> Db.newCommand "SELECT * FROM posts WHERE id = @id"
        |> Db.setParams [ "id", SqlType.Int postId ]
        |> Db.Async.querySingle Post.ofDataReader)

let getVotesAsync postId =
    withConnection (fun connection ->
        connection
        |> Db.newCommand "SELECT COUNT(*) AS post_count WHERE post_id = @id"
        |> Db.setParams [ "id", SqlType.Int postId ]
        |> Db.Async.querySingle (fun reader -> reader.ReadInt32 "post_count"))