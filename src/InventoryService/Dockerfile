FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/MessagingContracts/MessagingContracts.csproj src/MessagingContracts/
COPY src/InventoryService/InventoryService.csproj src/InventoryService/

RUN dotnet restore src/InventoryService/InventoryService.csproj

COPY src/MessagingContracts/ src/MessagingContracts/
COPY src/InventoryService/ src/InventoryService/

RUN dotnet publish src/InventoryService/InventoryService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "InventoryService.dll"]