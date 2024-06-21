# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Download and install the CA certificate
RUN apt-get update && apt-get install -y wget ca-certificates
RUN wget https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem -O /usr/local/share/ca-certificates/global-bundle.pem
RUN update-ca-certificates

# Use the official .NET SDK to build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Order_API/Order_API.csproj", "Order_API/"]
RUN dotnet restore "Order_API/Order_API.csproj"
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
