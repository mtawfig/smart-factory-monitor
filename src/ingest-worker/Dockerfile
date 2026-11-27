# Ingest Worker — multi-stage build. Build context is the repository root:
#   docker build -f src/ingest-worker/Dockerfile -t smartfactory/ingest-worker .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# Why .editorconfig: analyzer settings (e.g. migrations marked as generated code) must match the CI build.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/building-blocks/SmartFactory.BuildingBlocks/*.csproj src/building-blocks/SmartFactory.BuildingBlocks/
COPY src/ingest-worker/SmartFactory.Ingest.Worker/*.csproj src/ingest-worker/SmartFactory.Ingest.Worker/
RUN dotnet restore src/ingest-worker/SmartFactory.Ingest.Worker/SmartFactory.Ingest.Worker.csproj

COPY src/building-blocks/ src/building-blocks/
COPY src/ingest-worker/ src/ingest-worker/
RUN dotnet publish src/ingest-worker/SmartFactory.Ingest.Worker/SmartFactory.Ingest.Worker.csproj \
    -c Release -o /app --no-restore

# Why aspnet (not the smaller runtime image): the worker serves no HTTP itself, but it references
# BuildingBlocks, which declares a FrameworkReference to Microsoft.AspNetCore.App (auth, tenancy, telemetry).
# On the plain runtime image the host exits at start with "framework Microsoft.AspNetCore.App not found";
# the CI smoke test caught exactly that. Splitting BuildingBlocks would allow the smaller image later.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV DOTNET_gcServer=0 SMARTFACTORY_DATA=synthetic
USER $APP_UID
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SmartFactory.Ingest.Worker.dll"]
