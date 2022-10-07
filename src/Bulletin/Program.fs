open System
open Falco
open Falco.Routing
open Falco.HostBuilder

[<EntryPoint>]
let main args =
    webHost args { endpoints [ get "/ping" (Response.ofPlainText "pong") ] }
    0
