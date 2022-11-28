open System
open Fable.Remoting.Server
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
      .For<RssFeed>()
      .UniqueIndex(UniqueIndexType.Computed, (fun feed -> box feed.RssFeedUrl))
    |> ignore

    options
      .Schema
      .For<Post>()
      .FullTextIndex(Lambda.ofArity1 <@ fun post -> box post.Headline @>)
      .UniqueIndex(UniqueIndexType.Computed, (fun post -> box post.Link))
      .ForeignKey<RssFeed>(fun post -> post.Feed)
    |> ignore

    options
      .Schema
      .For<FeedSubscription>()
      .ForeignKey<User>(fun subscription -> subscription.UserId)
      .ForeignKey<RssFeed>(fun subscription -> subscription.FeedId)
    |> ignore

    options.AutoCreateSchemaObjects <- AutoCreate.CreateOrUpdate

let configureServices (serviceCollection: IServiceCollection) =
  serviceCollection.AddMarten(configureStore) |> ignore

  serviceCollection
    .AddScoped<IScopedBackgroundService, RssWorker>()
    .AddHostedService<RssWorkerBackgroundService>()
  |> ignore

  serviceCollection

let errorHandler (ex: Exception) (routeInfo: RouteInfo<HttpContext>) =
  // TODO: I need to do proper logging.
  printfn $"Error at %s{routeInfo.path} on method %s{routeInfo.methodName}"
  Propagate {| msg = ex.Message |}

let routeBuilder (typeName: string) (methodName: string) = $"/api/{typeName}/{methodName}"

let remotingOptions () =
  Remoting.createApi ()
  |> Remoting.withErrorHandler errorHandler
  |> Remoting.withRouteBuilder routeBuilder

let unsecuredServerApi: HttpHandler =
  remotingOptions ()
  |> Remoting.fromContext CompositionRoot.unsecuredServerApi
  |> Remoting.buildHttpHandler

let securedServerApi: HttpHandler =
  remotingOptions ()
  |> Remoting.fromContext CompositionRoot.securedServerApi
  |> Remoting.buildHttpHandler

let handler: HttpHandler =
  choose [
    unsecuredServerApi
    Auth.requiresAuthentication (setStatusCode 401) >=> securedServerApi
  ]

let app = application {
  url "http://*:5000"
  service_config configureServices
  use_cookies_authentication_with_config cookieAuthenticationOptions
  use_static "public"
  use_response_caching
  use_router handler
}

run app
