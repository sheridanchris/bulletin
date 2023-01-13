FROM mcr.microsoft.com/dotnet/sdk:7.0 as build

RUN curl -sL https://deb.nodesource.com/setup_16.x | bash
RUN apt-get update && apt-get install -y nodejs

FROM build AS publish
WORKDIR /app

COPY . .

RUN dotnet fsi ./build.fsx

FROM build AS final
WORKDIR /app
COPY --from=publish /app/deploy .
ENTRYPOINT ["dotnet", "Server.dll"]
