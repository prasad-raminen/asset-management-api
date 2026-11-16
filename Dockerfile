# syntax=docker/dockerfile:1

# ---- build ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the dependency layer is cached independently of source changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/AssetManagement.Domain/AssetManagement.Domain.csproj src/AssetManagement.Domain/
COPY src/AssetManagement.Application/AssetManagement.Application.csproj src/AssetManagement.Application/
COPY src/AssetManagement.Infrastructure/AssetManagement.Infrastructure.csproj src/AssetManagement.Infrastructure/
COPY src/AssetManagement.Api/AssetManagement.Api.csproj src/AssetManagement.Api/
RUN dotnet restore src/AssetManagement.Api/AssetManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/AssetManagement.Api/AssetManagement.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- runtime --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

# The .NET 8 images ship a non-root "app" user; never run the API as root.
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "AssetManagement.Api.dll"]
