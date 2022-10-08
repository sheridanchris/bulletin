open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.Google
open Falco
open Falco.Routing
open Falco.HostBuilder
open ScribanEngine
open System.IO
open Scriban

let configuration = configuration [||] {
    optional_json "appsettings.Development.json"
    required_json "appsettings.json"
}

let googleOptions: GoogleOptions -> unit =
    fun googleOptions ->
        googleOptions.ClientId <- configuration["Google.ClientId"]
        googleOptions.ClientSecret <- configuration["Google.Secret"]

let configureServices (views: Map<string, Template>) (serviceCollection: IServiceCollection) =
    serviceCollection.AddScoped<IViewEngine, ScribanViewEngine>(fun _ -> new ScribanViewEngine(views)) |> ignore
    serviceCollection.AddAuthentication().AddGoogle(googleOptions) |> ignore
    serviceCollection

let scribanViews =
    let root = Directory.GetCurrentDirectory()
    let viewsDirectory = Path.Combine(root, "views")

    let viewFromFile (file: string) =
        let viewName = Path.GetFileNameWithoutExtension(file)
        let viewContent = File.ReadAllText(file)
        let view = Template.Parse(viewContent)
        viewName, view 

    Directory.EnumerateFiles(viewsDirectory)
    |> Seq.map viewFromFile
    |> Map.ofSeq

[<EntryPoint>] 
let main args =
    webHost args {
        add_service (configureServices scribanViews)
        use_authentication
        use_authorization
        use_compression
        use_caching

        endpoints [ get "/ping" (Response.ofPlainText "pong") ]
    }
    0
