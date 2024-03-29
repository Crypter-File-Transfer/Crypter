FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source/

COPY ../*.sln .
COPY ../Crypter.Common/*.csproj ./Crypter.Common/
COPY ../Crypter.Common.Client/*.csproj ./Crypter.Common.Client/
COPY ../Crypter.Crypto.Common/*.csproj ./Crypter.Crypto.Common/
COPY ../Crypter.Crypto.Providers.Browser/*.csproj ./Crypter.Crypto.Providers.Browser/
COPY ../Crypter.Web/*.csproj ./Crypter.Web/

RUN dotnet restore Crypter.Web

COPY Crypter.Common/. ./Crypter.Common/
COPY Crypter.Common.Client/. ./Crypter.Common.Client/
COPY Crypter.Crypto.Common/. ./Crypter.Crypto.Common/
COPY Crypter.Crypto.Providers.Browser/. ./Crypter.Crypto.Providers.Browser/
COPY Crypter.Web/. ./Crypter.Web/

RUN dotnet publish Crypter.Web --no-restore --configuration release /p:TreatWarningsAsErrors=true /warnaserror --output /app/

FROM caddy:2.7-alpine AS webhost
COPY Crypter.Web/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/wwwroot/ /srv/
EXPOSE 80
EXPOSE 443
