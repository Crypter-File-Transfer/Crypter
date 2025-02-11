FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG VCS_URL
ENV VCS_URL=$VCS_URL
WORKDIR /source/

RUN dotnet tool install --global dotnet-ef --version 9.0.*
ENV PATH="${PATH}:/root/.dotnet/tools"

COPY ../*.sln ./
COPY ../Crypter.API/*.csproj ./Crypter.API/
COPY ../Crypter.Common/*.csproj ./Crypter.Common/
COPY ../Crypter.Core/*.csproj ./Crypter.Core/
COPY ../Crypter.Crypto.Common/*.csproj ./Crypter.Crypto.Common/
COPY ../Crypter.Crypto.Providers.Default/*.csproj ./Crypter.Crypto.Providers.Default/
COPY ../Crypter.DataAccess/*.csproj ./Crypter.DataAccess/

RUN dotnet restore Crypter.API --runtime linux-x64

COPY Crypter.API/. ./Crypter.API/
COPY Crypter.Common/. ./Crypter.Common/
COPY Crypter.Core/. ./Crypter.Core/
COPY Crypter.Crypto.Common/. ./Crypter.Crypto.Common/
COPY Crypter.Crypto.Providers.Default/. ./Crypter.Crypto.Providers.Default/
COPY Crypter.DataAccess/. ./Crypter.DataAccess/
COPY .git/. ./.git/

RUN dotnet publish Crypter.API --configuration release --no-self-contained /p:TreatWarningsAsErrors=true /warnaserror --output /app/ /p:Version=$GIT_VERSION_TAG /p:InformationalVersion=$GIT_VERSION_TAG_FULL
RUN dotnet-ef migrations bundle --project Crypter.DataAccess --startup-project Crypter.API --configuration release --no-build --target-runtime linux-x64 --output /app/efbundle --verbose

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
CMD ["dotnet", "Crypter.API.dll"]
