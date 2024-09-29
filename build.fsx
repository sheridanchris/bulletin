#r "nuget: Fun.Build, 1.1.7"

open Fun.Build

[<RequireQualifiedAccess>]
module Stages =
  let server = stage "watch server" { run "dotnet watch run --project src/Bulletin" }

  let tailwind =
    stage "tailwind" { run "npx tailwindcss -i styles.css -o ./src/Bulletin/public/tailwind.css --watch" }

pipeline "watch" {
  stage "watch server and tailwind" {
    Stages.server
    Stages.tailwind
    paralle
  }

  runIfOnlySpecified
}
