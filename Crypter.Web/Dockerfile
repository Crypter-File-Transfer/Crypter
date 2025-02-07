FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG GIT_VERSION_TAG=0.0.0
ARG GIT_VERSION_TAG_FULL=0.0.0
ARG VCS_URL
ENV VCS_URL=$VCS_URL

WORKDIR /source/

SHELL ["/bin/bash", "-c"]

RUN apt-get update -yq && apt-get upgrade -yq && apt-get install -yq curl git nano
RUN curl -fsSL https://deb.nodesource.com/setup_21.x | bash - && apt-get install -yq nodejs
RUN npm install --global pnpm \
    && SHELL=bash pnpm setup \
    && source /root/.bashrc

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
COPY .git/. ./.git/

RUN dotnet workload install wasm-tools
RUN dotnet publish Crypter.Web --no-restore --configuration release /p:TreatWarningsAsErrors=true /warnaserror --output /app/ /p:Version=$GIT_VERSION_TAG /p:InformationalVersion=$GIT_VERSION_TAG_FULL

FROM caddy:2.9-alpine AS webhost
COPY Crypter.Web/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/wwwroot/ /srv/
EXPOSE 80
EXPOSE 443
