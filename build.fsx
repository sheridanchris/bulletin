#r "nuget: Fun.Build, 0.2.9"

open Fun.Build

pipeline "Build Bulletin" {

  stage "restore tools and dependencies" {
    run "dotnet tool restore"
    run "dotnet restore"
    run "npm install"
  }

  stage "publish backend" {
    run "dotnet build src/Server/Server.fsproj -c Release -o ./deploy"
  }

  stage "publish frontend" {
    run "npm run build"
  }

  runIfOnlySpecified false
}
