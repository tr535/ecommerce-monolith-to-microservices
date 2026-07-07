FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ProductCatalogService.csproj", "./"]
RUN dotnet restore "ProductCatalogService.csproj"

COPY . .
RUN dotnet publish "ProductCatalogService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ProductCatalogService.dll"]