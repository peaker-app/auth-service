# Contexto de build: la raíz del monorepo (peaker-app), para incluir common-library.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY common-library/ common-library/
COPY auth-service/ auth-service/

RUN dotnet restore auth-service/AuthService/AuthService.API/AuthService.API.csproj
RUN dotnet publish auth-service/AuthService/AuthService.API/AuthService.API.csproj \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "AuthService.API.dll"]
