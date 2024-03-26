#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Food_Ordering_Web/Food_Ordering_Web.csproj", "Food_Ordering_Web/"]
COPY ["Food_Ordering_API/Food_Ordering_API.csproj", "Food_Ordering_API/"]
COPY ["Order_API/Order_API.csproj", "Order_API/"]
COPY ["Restaurant_API/Restaurant_API.csproj", "Restaurant_API/"]
RUN dotnet restore "Food_Ordering_Web/Food_Ordering_Web.csproj"
COPY . .
WORKDIR "/src/Food_Ordering_Web"
RUN dotnet build "Food_Ordering_Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Food_Ordering_Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Food_Ordering_Web.dll"]