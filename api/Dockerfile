FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY InventronAPI/InventronAPI.csproj InventronAPI/
RUN dotnet restore InventronAPI/InventronAPI.csproj
COPY InventronAPI/ InventronAPI/
RUN dotnet publish InventronAPI/InventronAPI.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventronAPI.dll"]
