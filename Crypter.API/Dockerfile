FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source/

COPY ../ ./
RUN dotnet restore

WORKDIR /source/Crypter.API
RUN dotnet publish -c release -o /app/

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Crypter.API.dll"]
