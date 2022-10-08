open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.Google
open Falco
open Falco.Routing
open Falco.HostBuilder

let configuration =
    configuration [||] {
        optional_json "appsettings.Development.json"
        required_json "appsettings.json"
    }

let googleOptions: GoogleOptions -> unit =
    fun googleOptions ->
        googleOptions.ClientId <- configuration["Google.ClientId"]
        googleOptions.ClientSecret <- configuration["Google.Secret"]

let configureServices (serviceCollection: IServiceCollection) =
    serviceCollection.AddAuthentication().AddGoogle(googleOptions) |> ignore
    serviceCollection

[<EntryPoint>]
let main args =
    webHost args {
        add_service configureServices
        use_authentication
        use_authorization
        use_compression
        use_caching

        endpoints [ get "/ping" (Response.ofPlainText "pong") ]
    }

    0
