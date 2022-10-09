module Persistence

open Microsoft.Data.Sqlite
open Domain
open SqlFun.GeneratorConfig
open SqlFun.Queries
open SqlFun
open SqlFun.Transforms.Conventions

let createConnection () = new SqliteConnection("Data Source=local.db")
let generatorConfig = createDefaultConfig createConnection
let sql commandText = sql generatorConfig commandText

let run f = DbAction.run createConnection f
let runAsync f = AsyncDb.run createConnection f

let insertPost: Post -> AsyncDb<int> =
    sql "insert into posts (Id, Headline, Link, Poster, CreatedAt, UpdatedAt)
         values (@Id, @Headline, @Link, @Poster, @CreatedAt, @UpdatedAt)"

let getPosts: unit -> AsyncDb<Post list> =
    sql "select p.Id, p.Headline, p.Link, p.Poster, p.CreatedAt, p.UpdatedAt from posts as p 
         left join votes as vote on post.Id = vote.PostId
         order by post.Id, vote.PostId"
    >> AsyncDb.map combine<_, Vote>