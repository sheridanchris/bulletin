open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.Extensions.Configuration
open Falco
open Falco.Routing
open Falco.HostBuilder
open Scriban
open ScribanEngine
open System.IO
open DbUp
open Worker
open Persistence
open Npgsql

Dapper.FSharp.OptionTypes.register()

let configuration = configuration [||] { add_env }

DeployChanges
    .To
    .PostgresqlDatabase(configuration.GetConnectionString("Postgresql"))
    .WithScriptsFromFileSystem("migrations")
    .LogToConsole()
    .Build()
    .PerformUpgrade() |> ignore

let googleOptions: GoogleOptions -> unit =
    fun googleOptions ->
        googleOptions.ClientId <- configuration["GOOGLE_CLIENT_ID"]
        googleOptions.ClientSecret <- configuration["GOOGLE_SECRET"]

let configureServices (views: Map<string, Template>) (serviceCollection: IServiceCollection) =
    let connectionFactory: DbConnectionFactory = fun () -> new NpgsqlConnection(configuration.GetConnectionString("Postgresql"))

    // serviceCollection.AddAuthentication().AddGoogle(googleOptions) |> ignore

    serviceCollection
        .AddSingleton<DbConnectionFactory>(connectionFactory)
        .AddScoped<IViewEngine, ScribanViewEngine>(fun _ -> new ScribanViewEngine(views))
        .AddHostedService<RssWorker>()

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
        // use_authentication
        // use_authorization
        use_compression
        use_caching
        use_static_files

        endpoints
            [ get "/" Handlers.postsHandler
              get "/ping" (Response.ofPlainText "pong") ]
    }

    0
