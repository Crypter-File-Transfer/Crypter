FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source/

RUN dotnet tool install --global dotnet-references
ENV PATH="${PATH}:/root/.dotnet/tools"

COPY *.sln ./
COPY */*.csproj ./

RUN dotnet references fix --entry-point ./ --working-directory ./ --remove-unreferenced-project-files
RUN dotnet restore Crypter.API

COPY ./ ./
RUN dotnet publish Crypter.API --no-restore --configuration release --output /app/

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Crypter.API.dll"]
