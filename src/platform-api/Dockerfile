# Platform API — multi-stage build. Build context is the repository root:
#   docker build -f src/platform-api/Dockerfile -t smartfactory/platform-api .
# Why root context: the host references src/building-blocks, outside this folder.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# Restore first with only project/props files so this layer is cached across code changes.
# Why .editorconfig: analyzer settings (e.g. migrations marked as generated code) must match the CI build.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/building-blocks/SmartFactory.BuildingBlocks/*.csproj src/building-blocks/SmartFactory.BuildingBlocks/
COPY src/platform-api/SmartFactory.Platform.Api/*.csproj src/platform-api/SmartFactory.Platform.Api/
COPY src/platform-api/Modules/SmartFactory.Modules.Assets/*.csproj src/platform-api/Modules/SmartFactory.Modules.Assets/
COPY src/platform-api/Modules/SmartFactory.Modules.Maintenance/*.csproj src/platform-api/Modules/SmartFactory.Modules.Maintenance/
COPY src/platform-api/Modules/SmartFactory.Modules.Inventory/*.csproj src/platform-api/Modules/SmartFactory.Modules.Inventory/
COPY src/platform-api/Modules/SmartFactory.Modules.Audit/*.csproj src/platform-api/Modules/SmartFactory.Modules.Audit/
RUN dotnet restore src/platform-api/SmartFactory.Platform.Api/SmartFactory.Platform.Api.csproj

COPY src/building-blocks/ src/building-blocks/
COPY src/platform-api/ src/platform-api/
RUN dotnet publish src/platform-api/SmartFactory.Platform.Api/SmartFactory.Platform.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0 \
    SMARTFACTORY_DATA=synthetic
# Why: attachments are written by the non-root app user. Creating the folder here with that owner means a named
# volume mounted over it (Compose) starts out writable; Azure uses Blob Storage instead (week 9).
RUN mkdir -p /var/lib/smartfactory/attachments && chown "$APP_UID" /var/lib/smartfactory/attachments
ENV Attachments__LocalRoot=/var/lib/smartfactory/attachments
# Why: the aspnet image ships a non-root user; Container Apps and Compose both run it fine.
USER $APP_UID
EXPOSE 8080
COPY --from=build /app .
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=5 \
    CMD ["dotnet", "SmartFactory.Platform.Api.dll", "--health-probe"]
ENTRYPOINT ["dotnet", "SmartFactory.Platform.Api.dll"]
