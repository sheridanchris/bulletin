module Remoting

open Fable.Remoting.Client
open Shared

let unsecuredServerApi: UnsecuredServerApi =
  Remoting.createApi ()
  |> Remoting.withBaseUrl "/api"
  |> Remoting.buildProxy<UnsecuredServerApi>

let securedServerApi: SecuredServerApi =
  Remoting.createApi ()
  |> Remoting.withBaseUrl "/api"
  |> Remoting.buildProxy<SecuredServerApi>
