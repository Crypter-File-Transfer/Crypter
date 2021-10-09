# Development Environment Setup

This section details how to setup a basic development environment, without containers.

## MySQL

Download MySQL: https://dev.mysql.com/downloads/installer/

Make sure to include MySQL Workbench in the installation.

You will be prompted to enter a password for the `root` user during installation.  This password will need to be copied into your local `Crypter.API/appsettings.json` and `Crypter.Console/appsettings.json` files whenever you run these apps.

After installation, a Windows service named `MySQL80` will exist on your system.  Make sure this service is running anytime you want to access your local database.

## Crypter.API

1. Review and configure the `.\Crypter.API\appsettings.json` file.
2. Open `.\Crypter.API` in a terminal.
3. Invoke `dotnet run`.

This will run the API on `https://localhost:5001`.

## Crypter.Web

1. Review and configure the `.\Crypter.Web\appsettings.json` file.
2. From within Visual Studio, verify you have the `Crypter.sln` solution loaded in the Solution Explorer.  This is as opposed to having the Solution Explorer set to *Folder View*.
3. Set `Crypter.Web` as the startup project in Visual Studio.
4. Either run the startup project by clicking the green arrow in Visual Studio, or by pressing F5.

## Crypter.Console

1. Review and configure the `.\Crypter.Console\appsettings.json` file.
2. Build the project.
3. Open `.\Crypter.Console\bin\Debug\net5.0` in a terminal.
4. Invoke `.\Crypter.Console.exe`.  This will run the program and output a help menu.
