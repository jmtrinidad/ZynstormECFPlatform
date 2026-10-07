# syntax=docker/dockerfile:1

# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
# For more information, please see https://aka.ms/containercompat

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Instalar dependencias nativas para Npgsql/Postgres
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar primero solo los .csproj del grafo de Web.Api: la capa del restore se
# reutiliza mientras no cambien las dependencias.
COPY ZynstormECFPlatform.Common/ZynstormECFPlatform.Common.csproj ZynstormECFPlatform.Common/
COPY ZynstormECFPlatform.Core/ZynstormECFPlatform.Core.csproj ZynstormECFPlatform.Core/
COPY ZynstormECFPlatform.Dtos/ZynstormECFPlatform.Dtos.csproj ZynstormECFPlatform.Dtos/
COPY ZynstormECFPlatform.Abstractions/ZynstormECFPlatform.Abstractions.csproj ZynstormECFPlatform.Abstractions/
COPY ZynstormECFPlatform.Schemas/ZynstormECFPlatform.Schemas.csproj ZynstormECFPlatform.Schemas/
COPY ZynstormECFPlatform.Data/ZynstormECFPlatform.Data.csproj ZynstormECFPlatform.Data/
COPY ZynstormECFPlatform.Data.Services/ZynstormECFPlatform.Data.Services.csproj ZynstormECFPlatform.Data.Services/
COPY ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj ZynstormECFPlatform.Services/
COPY ZynstormECFPlatform.Mappings/ZynstormECFPlatform.Mappings.csproj ZynstormECFPlatform.Mappings/
COPY ZynstormECFPlatform.Reports/ZynstormECFPlatform.Reports.csproj ZynstormECFPlatform.Reports/
COPY ZynstormECFPlatform.Web.Api/ZynstormECFPlatform.Web.Api.csproj ZynstormECFPlatform.Web.Api/

# Restore para la arquitectura del VPS.
# Sin --mount=type=cache: el restore y el publish --no-restore corren en RUN distintos y,
# con cache de capas (type=gha), el restore se salta y un cache mount llegaria vacio al
# publish (NETSDK1064). Los paquetes deben quedar dentro de la capa del restore.
RUN dotnet restore "./ZynstormECFPlatform.Web.Api/ZynstormECFPlatform.Web.Api.csproj" \
    -r linux-x64

COPY . .

# Publish once; dotnet publish already includes compilation.
WORKDIR "/src/ZynstormECFPlatform.Web.Api"
RUN dotnet publish "./ZynstormECFPlatform.Web.Api.csproj" \
    --no-restore \
    -r linux-x64 \
    --self-contained false \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    -maxcpucount:2 \
    -p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set culture to invariant if needed or install ICU data (standard for .NET on Linux)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "ZynstormECFPlatform.Web.Api.dll"]
