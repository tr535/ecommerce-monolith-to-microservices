FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/MessagingContracts/MessagingContracts.csproj src/MessagingContracts/
COPY src/OrderService/OrderService.csproj src/OrderService/

RUN dotnet restore src/OrderService/OrderService.csproj

COPY src/MessagingContracts/ src/MessagingContracts/
COPY src/OrderService/ src/OrderService/

RUN dotnet publish src/OrderService/OrderService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OrderService.dll"]