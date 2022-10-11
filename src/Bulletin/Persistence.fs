module Persistence

open Microsoft.Data.Sqlite
open Domain
open SqlFun
open SqlFun.Sqlite
open SqlFun.Queries
open SqlFun.Transforms
open SqlFun.GeneratorConfig

let createConnection () = new SqliteConnection("Data Source=local.db")
let generatorConfig = createDefaultConfig createConnection |> representDatesAsStrings
let sql commandText = sql generatorConfig commandText

let run f = DbAction.run createConnection f
let runAsync f = AsyncDb.run createConnection f

let insertPost: Post -> AsyncDb<unit> =
    sql "insert into posts (Headline, Description, Link, Poster, PublishedDate)
         values (@Headline, @Description, @Link, @Poster, @PublishedDate)"

let insertPosts: Post list -> AsyncDb<unit> =
    sql "insert into posts (Headline, Description, Link, Poster, PublishedDate)
         values (@Headline, @Description, @Link, @Poster, @PublishedDate)"

let getPosts: unit -> AsyncDb<Post list> =
    sql "select p.Id, p.Headline, p.Link, p.Poster, p.CreatedAt, p.UpdatedAt from posts as p 
         left join votes as vote on post.Id = vote.PostId
         left join users as user on user.id = post.AuthorId
         order by post.Id, vote.PostId"
    >> AsyncDb.map (Conventions.combine<_, Vote> >-> Conventions.combine<_, User>)