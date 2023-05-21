# Development Environment Setup

This section details how to setup a basic development environment.

## Visual Studio

As you may have guessed, some version of Visual Studio is required in order to work on Crypter.

[Visual Studio Download Page](https://visualstudio.microsoft.com/)

I recommend using Visual Studio 2022 for Crypter development.
The only workload you need to install is `ASP.NET and web development`.

Make sure these individual components are also installed:
* `.NET 7.0 Runtime` 
* `.NET WebAssembly build tools`

## Docker

I highly recommend installing Docker Desktop, though it is not required.

[Get Started with Docker](https://www.docker.com/get-started)

Refer to the commands below if you decide to run the application using Docker.

### Run everything

`docker-compose --profile dev up`

You do not need to modify anything in the project for this to work.

Just navigate to `https://localhost`.

### Run just the database

`docker-compose --profile db up`

It's expected that you will run Crypter.API and Crypter.Web through Visual Studio or the command line.

Instructions on running these web applications "the old fashioned way" are below.

## PostgreSQL - Additional Tools

Download and install PGAdmin: [PGAdmin Downloads Page](https://www.pgadmin.org/download/)

## Crypter.API

### Using Visual Studio

1. Review and configure the `.\Crypter.API\appsettings.json` file.
2. From within Visual Studio, verify you have the `Crypter.sln` solution loaded in the Solution Explorer. This is as opposed to having the Solution Explorer set to *Folder View*.
3. Set `Crypter.API` as the startup project in Visual Studio.
4. Either run the startup project by clicking the green arrow in Visual Studio, or by pressing F5.

This will run the API on `https://localhost:5003`.
Make sure you navigate to this address in your browser before trying to use the API.
You may need to acknowledge the API's self-signed certificate.

## Crypter.Web

1. Review and configure the `.\Crypter.Web\wwwroot\appsettings.Development.json` file.
2. Open `.\Crypter.Web` in a terminal.
3. Invoke `dotnet run`.

This will run the web app on `https://localhost:5001`.
