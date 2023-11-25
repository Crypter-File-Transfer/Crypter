FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source/

RUN dotnet tool install --global dotnet-references
ENV PATH="${PATH}:/root/.dotnet/tools"

COPY ../*.sln ./
COPY ../*/*.csproj ./

RUN dotnet-references fix --entry-point ./Crypter.sln --working-directory ./ --remove-unreferenced-project-files
RUN dotnet restore Crypter.Web

COPY ./ ./
RUN dotnet publish Crypter.Web --no-restore --configuration release /p:TreatWarningsAsErrors=true /warnaserror --output /app/

FROM caddy:2.6-alpine AS webhost
COPY Crypter.Web/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/wwwroot/ /srv/
EXPOSE 80
EXPOSE 443
