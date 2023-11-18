# Deploying PostgreSQL in a Production Environment

This document describes how to setup PostgreSQL using docker-compose.

It also describes how to setup the `crypter` and `crypter_hangfire` databases.
Both databases are required.

## Steps

1. Copy the [./Containers/PostgreSQL](../../../Containers/PostgreSQL) directory to the database server.
2. Review and configure the `.env` file.
3. Modify the user passwords in the `./postgres-init-files/init.sh` file, lines 10 and 25.
4. Invoke `docker-compose --profile db up`.

## Crypter schema

After getting the PostgreSQL container running, the `crypter` database will exist, but it will not have any tables.
There are two ways to create these tables.

### Method 1 - Development

Crypter.API will automatically create any missing tables in the database when it is run in development mode.
All you need to do is set the correct connection string in the project's `appsettings.json` and run the API.

A few drawbacks to this method are:
* This requires the API connect to the database with a user that has total control over the database schema. This violates the principal of least privilege.
* Migrations are not automatically handled. If the schema changes, it will be easier to just delete your database and start fresh with the latest schema.

### Method 2 - Production

To create the database:

 1. Verify the user configured in the `Crypter.API/appsettings.json` file's `DefaultConnection` connection string is a superuser.
 2. Open the Package Manager Console in Visual Studio.
 3. Select `Crypter.API` as the default project.
 4. Invoke `Update-Database` to create the most recent version of the database.
 5. Undo the change you made to `Crypter.API/appsettings.json`.

To create a database migration:

 1. Open the Package Manager Console in Visual Studio.
 2. Select `Crypter.API` as the startup project.
 3. Select `Crypter.Core` as the default project.
 4. Invoke `Add-Migration {MigrationName}`

To migrate the database:

 1. Open the Package Manager Console in Visual Studio.
 2. Select `Crypter.API` as the default project.
 3. Use `Script-Migration` to produce a migration script. You may need to use the `-From`, `-To`, or `-Idempotent` arguments to get what you really need.
    * Example: `Script-Migration -From {MigrationName} -To {MigrationName}`
 4. Backup the database using `pg_dump dbname > outfile`.
 5. Run the migration script as a superuser.

## Hangfire Notes + Steps

Crypter uses [Hangfire.io](https://www.hangfire.io/), which requires it's own database instance.
Although the SQL in `init.sh` will create a `crypter_hangfire` database, it will not contain any of the tables or sequences expected by Hangfire.

I recommend letting Hangfire take care of it's own needs.
Running `Crypter.API` is sufficient to get this database scaffolded, assuming the `init.sh` script has already been run.
