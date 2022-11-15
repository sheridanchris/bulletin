FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443


FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore "src/Server/Server.fsproj" --no-cache
RUN dotnet build "src/Server/Server.fsproj" -c Release -o /app/build /maxcpucount:4

FROM build AS publish
RUN dotnet publish "src/Server/Server.fsproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Server.dll"]
