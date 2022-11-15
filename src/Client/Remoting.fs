module Remoting

open Fable.Remoting.Client
open Shared

let serverApi: ServerApi =
  Remoting.createApi ()
  |> Remoting.withBaseUrl "/api"
  |> Remoting.buildProxy<ServerApi>

