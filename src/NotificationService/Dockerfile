FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/MessagingContracts/MessagingContracts.csproj src/MessagingContracts/
COPY src/NotificationService/NotificationService.csproj src/NotificationService/

RUN dotnet restore src/NotificationService/NotificationService.csproj

COPY src/MessagingContracts/ src/MessagingContracts/
COPY src/NotificationService/ src/NotificationService/

RUN dotnet publish src/NotificationService/NotificationService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "NotificationService.dll"]