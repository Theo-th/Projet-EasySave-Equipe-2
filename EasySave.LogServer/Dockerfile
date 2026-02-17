# Utilisation de l'image SDK pour construire
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EasySave.LogServer.csproj", "./"]
RUN dotnet restore "EasySave.LogServer.csproj"
COPY . .
RUN dotnet publish "EasySave.LogServer.csproj" -c Release -o /app/publish

# Utilisation de l'image Runtime pour exécuter
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
# Création du dossier pour les logs
RUN mkdir -p /app/centralized_logs
ENTRYPOINT ["dotnet", "EasySave.LogServer.dll"]