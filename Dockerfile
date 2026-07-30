ARG DOTNET_SDK_VERSION=10.0.302
ARG ASPNET_VERSION=10.0.7

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
WORKDIR /source

COPY Directory.Build.props global.json NuGet.Config ./
COPY src/OperationsHub.Domain/OperationsHub.Domain.csproj src/OperationsHub.Domain/
COPY src/OperationsHub.Application/OperationsHub.Application.csproj src/OperationsHub.Application/
COPY src/OperationsHub.Infrastructure/OperationsHub.Infrastructure.csproj src/OperationsHub.Infrastructure/
COPY src/OperationsHub.Web/OperationsHub.Web.csproj src/OperationsHub.Web/
RUN dotnet restore src/OperationsHub.Web/OperationsHub.Web.csproj

COPY src/ src/
RUN dotnet publish src/OperationsHub.Web/OperationsHub.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${ASPNET_VERSION} AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir --parents /var/lib/operationshub/keys \
    && chown --recursive "${APP_UID}:${APP_UID}" /var/lib/operationshub

COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER ${APP_UID}

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent --show-error http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "OperationsHub.Web.dll"]
