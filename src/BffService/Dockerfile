FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["BffService.csproj", "."]
RUN dotnet restore "BffService.csproj"

COPY . .
RUN dotnet publish "BffService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "BffService.dll"]