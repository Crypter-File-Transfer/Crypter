# Development Environment Setup

This section details how to setup a basic development environment.

## Visual Studio

As you may have guessed, some version of Visual Studio is required in order to work on Crypter.

[Visual Studio Download Page](https://visualstudio.microsoft.com/)

I recommend using Visual Studio 2022 for Crypter development.
The only workload you need to install is `ASP.NET and web development`.

Make sure these individual components are also installed:
* `.NET 5.0 Runtime`
* `.NET 6.0 Runtime` (for future use)
* `.NET WebAssembly build tools`

Visual Studio 2019 should still work fine for regular development, but do not publish Crypter.Web using the project's currently saved publish profile.
This publish profile contains options which were added in Visual Studio 2022.


## Docker

Docker is required to run the PostgreSQL database. Get Docker setup on your development machine.

[Get Started with Docker](https://www.docker.com/get-started)

## PostgreSQL

Download and install PGAdmin: [PGAdmin Downloads Page](https://www.pgadmin.org/download/)

Follow the instructions for a production deployment to get the container running on your development machine.
Those instructions are located [here](../Production/Deployment/PostgreSQL.md).

If you would rather not use Docker or Docker-Compose, you are free to setup PostgreSQL on your own.
However, you must still refer to the initialization script located [here](../../Containers/PostgreSQL/postgres-init-files/init.sh).
This script does a few things, including create some databases and a database user.
You will not be able to create a database schema without executing this script.

## Crypter.API

1. Review and configure the `.\Crypter.API\appsettings.json` file.
2. From within Visual Studio, verify you have the `Crypter.sln` solution loaded in the Solution Explorer. This is as opposed to having the Solution Explorer set to *Folder View*.
3. Set `Crypter.API` as the startup project in Visual Studio.
4. Either run the startup project by clicking the green arrow in Visual Studio, or by pressing F5.

This will run the API on `https://localhost:44324`.
Make sure you navigate to this address in your browser before trying to use the API.
You will need to acknowledge the API's self-signed certificate.

## Crypter.Web

1. Review and configure the `.\Crypter.Web\appsettings.json` file.
2. Open `.\Crypter.Web` in a terminal.
3. Invoke `dotnet run`.

This will run the web app on `https://localhost:5001`.

## Crypter.Console

1. Review and configure the `.\Crypter.Console\appsettings.json` file.
2. Build the project.
3. Open `.\Crypter.Console\bin\Debug\net5.0` in a terminal.
4. Invoke `.\Crypter.Console.exe`. This will run the program and output a help menu.
