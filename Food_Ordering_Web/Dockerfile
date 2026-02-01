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
COPY ["Food_Ordering_Web/Food_Ordering_Web.csproj", "Food_Ordering_Web/"]
COPY ["Food_Ordering_API/Food_Ordering_API.csproj", "Food_Ordering_API/"]
COPY ["Order_API/Order_API.csproj", "Order_API/"]
COPY ["Restaurant_API/Restaurant_API.csproj", "Restaurant_API/"]
COPY ["Menu_API/Menu_API.csproj", "Menu_API/"]
COPY ["Kitchen_Web/Kitchen_Web.csproj", "Kitchen_Web/"]

RUN dotnet restore "Food_Ordering_Web/Food_Ordering_Web.csproj" --disable-parallel

COPY . .
WORKDIR "/src/Food_Ordering_Web"
RUN dotnet build "Food_Ordering_Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Food_Ordering_Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Food_Ordering_Web.dll"]
