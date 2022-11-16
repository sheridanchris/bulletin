open System
open Fable.Remoting.Server
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Giraffe
open Worker
open Marten
open Data
open Marten.Services
open Marten.Schema
open Weasel.Core
open System.Text.Json.Serialization
open InitialData
open Fable.Remoting.Giraffe
open Saturn

let configuration: IConfiguration =
  ConfigurationBuilder().AddEnvironmentVariables().Build()

let cookieAuthenticationOptions: CookieAuthenticationOptions -> unit =
  fun options ->
    options.Cookie.Name <- "Session"
    options.Cookie.HttpOnly <- true
    options.Cookie.SameSite <- SameSiteMode.Strict

let configureStore: StoreOptions -> unit =
  fun options ->
    options.Connection(configuration.GetConnectionString("Postgresql"))

    let serializer =
      SystemTextJsonSerializer(EnumStorage = EnumStorage.AsString, Casing = Casing.CamelCase)

    serializer.Customize(fun options -> options.Converters.Add(JsonFSharpConverter()))
    options.Serializer(serializer)

    options
      .Schema
      .For<Post>()
      .FullTextIndex(Lambda.ofArity1 <@ fun post -> box post.Headline @>)
      .UniqueIndex(UniqueIndexType.Computed, (fun post -> box post.Link))
    |> ignore

    options
      .Schema
      .For<PostVote>()
      .ForeignKey<User>(fun vote -> vote.VoterId)
      .ForeignKey<Post>(fun vote -> vote.PostId)
    |> ignore

    options.AutoCreateSchemaObjects <- AutoCreate.CreateOrUpdate

let configureServices (serviceCollection: IServiceCollection) =
  serviceCollection.AddMarten(configureStore).InitializeWith(InitialData())
  |> ignore

  serviceCollection
    .AddScoped<IScopedBackgroundService, RssWorker>()
    .AddHostedService<RssWorkerBackgroundService>()
  |> ignore

  serviceCollection

type CustomError = { errorMsg: string }

let errorHandler (ex: Exception) (routeInfo: RouteInfo<HttpContext>) =
  printfn $"Error at %s{routeInfo.path} on method %s{routeInfo.methodName}"

  match ex with
  | ex ->
    let customError = { errorMsg = ex.Message }
    Propagate customError

let routeBuilder (typeName: string) (methodName: string) = $"/api/{typeName}/{methodName}"

let remotingHandler: HttpHandler =
  Remoting.createApi ()
  |> Remoting.withErrorHandler errorHandler
  |> Remoting.withRouteBuilder routeBuilder
  |> Remoting.fromContext Api.serverApi
  |> Remoting.buildHttpHandler

let app = application {
  url "http://*:5000"
  service_config configureServices
  use_cookies_authentication_with_config cookieAuthenticationOptions
  use_static "public"
  use_response_caching
  use_router remotingHandler
}

run app
