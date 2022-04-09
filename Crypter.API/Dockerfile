FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Crypter.API/Crypter.API.csproj", "Crypter.API/"]
COPY ["Crypter.Contracts/Crypter.Contracts.csproj", "Crypter.Contracts/"]
COPY ["Crypter.Common/Crypter.Common.csproj", "Crypter.Common/"]
COPY ["Crypter.CryptoLib/Crypter.CryptoLib.csproj", "Crypter.CryptoLib/"]
COPY ["Crypter.Core/Crypter.Core.csproj", "Crypter.Core/"]
RUN dotnet restore "Crypter.API/Crypter.API.csproj"

COPY . .
WORKDIR "/src/Crypter.API"
RUN dotnet build "Crypter.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Crypter.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Crypter.API.dll"]
