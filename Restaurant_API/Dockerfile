#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0-jammy AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0-jammy AS build
WORKDIR /src

# --- NuGet TLS hardening (fix SSL_ERROR_SSL / bad record mac) ---
ENV DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT=false \
    NUGET_HTTP_PROTOCOL_VERSION=1.1 \
    NUGET_CERT_REVOCATION_MODE=offline \
    NUGET_XMLDOC_MODE=skip

RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates \
 && update-ca-certificates \
 && rm -rf /var/lib/apt/lists/*

# Copy csproj files needed for restore (include any referenced projects)
COPY ["Restaurant_API/Restaurant_API.csproj", "Restaurant_API/"]
COPY ["Order_API/Order_API.csproj", "Order_API/"]
COPY ["Menu_API/Menu_API.csproj", "Menu_API/"]

RUN dotnet restore "Restaurant_API/Restaurant_API.csproj" --disable-parallel

COPY . .
WORKDIR "/src/Restaurant_API"
RUN dotnet build "Restaurant_API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Restaurant_API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Restaurant_API.dll"]
