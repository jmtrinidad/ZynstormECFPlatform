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

# Copy the main project and all its dependencies
# It's usually better to copy all csproj files first for caching, but for simplicity we'll just copy everything
COPY . .

# Restore the application for the VPS architecture. The NuGet cache survives
# across builds without becoming part of the resulting image.
RUN --mount=type=cache,id=zynstormecf-nuget,target=/root/.nuget/packages \
    dotnet restore "./ZynstormECFPlatform.Web.Api/ZynstormECFPlatform.Web.Api.csproj" \
    -r linux-x64

# Publish once; dotnet publish already includes compilation.
WORKDIR "/src/ZynstormECFPlatform.Web.Api"
RUN --mount=type=cache,id=zynstormecf-nuget,target=/root/.nuget/packages \
    dotnet publish "./ZynstormECFPlatform.Web.Api.csproj" \
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
