# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-jammy AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET SDK to build the application
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

COPY ["Order_API/Order_API.csproj", "Order_API/"]
RUN dotnet restore "Order_API/Order_API.csproj" --disable-parallel

COPY . .
WORKDIR "/src/Order_API"
RUN dotnet build "Order_API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Order_API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage: copy the publish output to the base image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Order_API.dll"]
