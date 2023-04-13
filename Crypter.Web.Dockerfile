FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Crypter.Web/Crypter.Web.csproj", "Crypter.Web/"]
COPY ["Crypter.Crypto.Providers.Browser/Crypter.Crypto.Providers.Browser.csproj", "Crypter.Crypto.Providers.Browser/"]
COPY ["Crypter.Crypto.Common/Crypter.Crypto.Common.csproj", "Crypter.Crypto.Common/"]
COPY ["Crypter.Common/Crypter.Common.csproj", "Crypter.Common/"]
COPY ["Crypter.Common.Client/Crypter.Common.Client.csproj", "Crypter.Common.Client/"]
RUN dotnet restore "Crypter.Web/Crypter.Web.csproj"
COPY . .
WORKDIR "/src/Crypter.Web"
RUN dotnet build "Crypter.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Crypter.Web.csproj" -c Release -o /app/publish  /p:UseAppHost=false

#FROM nginx:alpine AS final
#WORKDIR /usr/share/nginx/html
#COPY --from=publish /app/publish/wwwroot .
#COPY nginx.conf /etc/nginx/nginx.conf
