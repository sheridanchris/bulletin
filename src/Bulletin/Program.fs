open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.FileProviders
open System.IO

Database.migrate ()

let endpoints = [
  get "/" (Handlers.entriesList false)
  get "/favorites" (Handlers.entriesList true)

  all "/feeds" [ GET, Handlers.viewFeeds; POST, Handlers.createFeed ]
  delete "/feeds/{id}" Handlers.deleteFeed
  put "/favorite/{id}" (Handlers.updateFavorite true)
  put "/unfavorite/{id}" (Handlers.updateFavorite false)
]

let builder = WebApplication.CreateBuilder()

builder.Services.AddSingleton<Database.DbConnectionFactory>(Database.dbConnectionFactory)
|> ignore

builder.Services.AddHttpClient() |> ignore
builder.Services.AddHostedService<Worker.FeedReaderService>() |> ignore

let app = builder.Build()

app.UseStaticFiles(
  StaticFileOptions(
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "public"))
  )
)
|> ignore

app.UseFalco(endpoints) |> ignore
app.Run()
