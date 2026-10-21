# Build context: the repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached while only code changes
COPY Directory.Build.props ./
COPY src/FitTrack.Api/FitTrack.Api.csproj src/FitTrack.Api/
RUN dotnet restore src/FitTrack.Api/FitTrack.Api.csproj

COPY src/FitTrack.Api/ src/FitTrack.Api/
RUN dotnet publish src/FitTrack.Api/FitTrack.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# The base image ships a non-root user
USER $APP_UID
ENTRYPOINT ["dotnet", "FitTrack.Api.dll"]
