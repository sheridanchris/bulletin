#r "nuget: Fun.Build, 0.4.0"
#r "nuget: dotenv.net, 3.1.2"

open Fun.Build
open dotenv.net
open System.IO

let environmentVariables = DotEnv.Read()

let environmentVariableOrDefault defaultValue envVariableKey =
  match environmentVariables.TryGetValue envVariableKey with
  | true, environmentVariable -> environmentVariable
  | false, _ -> defaultValue

let redisPassword = "REDIS_PASSWORD" |> environmentVariableOrDefault ""
let postgresPassword = "POSTGRES_PASSWORD" |> environmentVariableOrDefault ""

pipeline "dev" {
  envVars [
    "ConnectionStrings__Seq", "http://seq"
    "ConnectionStrings__Redis", $"localhost,password={redisPassword}"
    "ConnectionStrings__Postgresql", $"Host=localhost;Database=postgres;Username=postgres;Password={postgresPassword}"
    "ASPNETCORE_ENVIRONMENT", "Development"
  ]

  stage "build and install" {
    run "dotnet build"

    run (fun ctx ->
      if not (Directory.Exists("node_modules")) then
        ctx.RunCommand("npm install")
      else
        async { return Ok() })
  }

  stage "start container" {
    run "docker compose down"
    run "docker compose up -d --build"
  }

  stage "run client & server" {
    paralle
    stage "run server" { run "dotnet watch run --project ./src/Server/Server.fsproj" }
    stage "run client" { run "npm start" }
  }

  runIfOnlySpecified true
}
