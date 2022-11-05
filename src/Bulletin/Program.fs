open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.Extensions.Configuration
open Falco
open Falco.Routing
open Falco.HostBuilder
open Scriban
open ScribanEngine
open System.IO
open Worker
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Marten
open Data
open Marten.Services
open Marten.Schema
open Weasel.Core
open System.Text.Json.Serialization
open InitialData

let configuration = configuration [||] { add_env }

let authenticationOptions: AuthenticationOptions -> unit =
    fun authOptions ->
        authOptions.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
        authOptions.DefaultChallengeScheme <- GoogleDefaults.AuthenticationScheme

let googleOptions: GoogleOptions -> unit =
    fun googleOptions ->
        googleOptions.ClientId <- configuration["GOOGLE_CLIENT_ID"]
        googleOptions.ClientSecret <- configuration["GOOGLE_SECRET"]
        googleOptions.SaveTokens <- true
        googleOptions.CallbackPath <- "/google-callback"

let configureServices (views: Map<string, Template>) (serviceCollection: IServiceCollection) =
    serviceCollection
        .AddAuthentication(authenticationOptions)
        .AddCookie()
        .AddGoogle(googleOptions)
    |> ignore

    serviceCollection
        .AddMarten(fun (options: StoreOptions) ->
            options.Connection(configuration.GetConnectionString("Postgresql"))

            let serializer =
                SystemTextJsonSerializer(
                    EnumStorage = EnumStorage.AsString,
                    Casing = Casing.CamelCase
                )

            serializer.Customize(fun options -> options.Converters.Add(JsonFSharpConverter()))
            options.Serializer(serializer)

            options.RegisterDocumentType<NewsSource>()

            options
                .Schema
                .For<Post>()
                .FullTextIndex(Lambda.ofArity1 <@ fun post -> box post.Headline @>)
                .UniqueIndex(UniqueIndexType.Computed, (fun post -> box post.Link))
            |> ignore

            options
                .Schema
                .For<Comment>()
                .ForeignKey<User>(fun comment -> comment.AuthorId)
                .ForeignKey<Post>(fun post -> post.PostId)
            |> ignore

            options.Schema.For<Vote>().ForeignKey<User>(fun vote -> vote.VoterId) |> ignore)
        .InitializeWith(InitialData())
    |> ignore

    serviceCollection
        .AddScoped<IViewEngine, ScribanViewEngine>(fun _ -> ScribanViewEngine(views))
        .AddScoped<IScopedBackgroundService, RssWorker>()
        .AddHostedService<RssWorkerBackgroundService>()

let scribanViews =
    let root = Directory.GetCurrentDirectory()
    let viewsDirectory = Path.Combine(root, "views")

    let viewFromFile (file: string) =
        let viewName = Path.GetFileNameWithoutExtension(file)
        let viewContent = File.ReadAllText(file)
        let view = Template.Parse(viewContent)
        viewName, view

    Directory.EnumerateFiles(viewsDirectory) |> Seq.map viewFromFile |> Map.ofSeq

[<EntryPoint>]
let main args =
    webHost args {
        add_service (configureServices scribanViews)
        use_authentication
        use_authorization
        use_compression
        use_caching
        use_static_files

        endpoints
            [ get "/{ordering?}" (Middleware.withService Handlers.postsHandler)
              get "/{postId}/comments" (Middleware.withService Handlers.commentsHandler)
              get "/google-signin" Handlers.googleOAuthHandler ]

    // not_found (Response.withStatusCode 404 >> Response.ofHtmlString "404.html")
    }

    0
