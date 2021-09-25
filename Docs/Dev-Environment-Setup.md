# Developer Onboarding

## MySQL

Download MySQL: https://dev.mysql.com/downloads/installer/

Make sure to include MySQL Workbench in the installation.

You will be prompted to enter a password for the `root` user during installation.  This password will need to be copied into your local `Crypter.API/appsettings.json` and `Crypter.Console/appsettings.json` files whenever you run these apps.

After installation, a Windows service named `MySQL80` will exist on your system.  Make sure this service is running anytime you want to access your local database.

## Running Crypter.API

The way I run Crypter.API is to open a terminal, `CD` into the Crypter.API directory, then execute `$ dotnet run`.  This will run the API on `https://localhost:5001`.

## Running Crypter.Web

The way I run `Crypter.Web`, after launching the API, is by running the application through Visual Studio.  Notice the green arrow with the `IIS Express` label in the VS toolbar.  Just click that.
