open Falco
open Falco.Routing
open Falco.HostBuilder

let add x y = x + y

let addFive = add 5

[<EntryPoint>]
let main args =
    webHost args { endpoints [ get "/ping" (Response.ofPlainText "pong") ] }
    0
