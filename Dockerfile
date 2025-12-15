# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopiera lösning och källkod
COPY . .

# Restore och publish
RUN dotnet restore
RUN dotnet publish travelagency.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 4999
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "travelagency.dll"]
