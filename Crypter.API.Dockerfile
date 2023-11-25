FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source/

RUN dotnet tool install --global dotnet-references
RUN dotnet tool install --global dotnet-ef --version 7.0.*
ENV PATH="${PATH}:/root/.dotnet/tools"

COPY *.sln ./
COPY */*.csproj ./

RUN dotnet-references fix --entry-point ./Crypter.sln --working-directory ./ --remove-unreferenced-project-files
RUN dotnet restore Crypter.API --runtime linux-x64

COPY ./ ./
RUN dotnet publish Crypter.API --configuration release --no-self-contained /p:TreatWarningsAsErrors=true /warnaserror --output /app/
RUN dotnet-ef migrations bundle --project Crypter.DataAccess --startup-project Crypter.API --configuration release --no-build --target-runtime linux-x64 --output /app/efbundle --verbose

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
CMD ["dotnet", "Crypter.API.dll"]
