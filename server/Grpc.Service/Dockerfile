# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet@sha256:7c4246c1c384319346d45b3e24a10a21d5b6fc9b36a04790e1588148ff8055b0 AS base
WORKDIR /app

# Install grpc_health_probe as root
# sha256:b92eeb7e9ffbc3b156f85f3bb18129bd18a27f739330f6e9d3fcb630898df442
USER root
RUN apt-get update && apt-get install -y curl \
    && curl -L "https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/v0.4.42/grpc_health_probe-linux-amd64" \
       -o /usr/local/bin/grpc_health_probe \
    && echo "b92eeb7e9ffbc3b156f85f3bb18129bd18a27f739330f6e9d3fcb630898df442  /usr/local/bin/grpc_health_probe" | sha256sum -c - \
    && chmod +x /usr/local/bin/grpc_health_probe \
    && apt-get purge -y curl \
    && rm -rf /var/lib/apt/lists/*

# Switch back to the app user
USER $APP_UID
EXPOSE 8080
EXPOSE 8081

ARG AUTHORS
ARG VERSION
ARG TITLE
ARG DESCRIPTION
ARG BUILD_DATE

FROM mcr.microsoft.com/dotnet/sdk@sha256:c7445f141c04f1a6b454181bd098dcfa606c61ba0bd213d0a702489e5bd4cd71 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Grpc.Service/Grpc.Service.csproj", "Grpc.Service/"]
COPY ["Grpc.Common/Grpc.Common.csproj", "Grpc.Common/"]
COPY ["Grpc.Data/Grpc.Data.csproj", "Grpc.Data/"]
RUN dotnet restore "./Grpc.Service/Grpc.Service.csproj"
COPY . .
WORKDIR "/src/Grpc.Service"
RUN dotnet build "./Grpc.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Grpc.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

LABEL \
    org.opencontainers.image.created="${BUILD_DATE}" \
    org.opencontainers.image.authors="${AUTHORS}" \
    org.opencontainers.image.title="${TITLE}" \
    org.opencontainers.image.description="${DESCRIPTION}" \
    org.opencontainers.image.version="${VERSION}" \
    org.opencontainers.image.licenses="MIT"

ENTRYPOINT ["dotnet", "Grpc.Service.dll"]
