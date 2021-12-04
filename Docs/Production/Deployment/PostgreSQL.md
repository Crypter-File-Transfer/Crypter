# Deploying PostgreSQL in a Production Environment

This document describes how to setup PostgreSQL using docker-compose.

It also describes how to setup the `crypter` and `crypter_hangfire` databases.
Both databases are required.

## Steps

1. Copy the [./Containers/PostgreSQL](../../../Containers/PostgreSQL) directory to the database server.
2. Review and configure the `.env` file.
3. Modify the user password in the `./postgres-init-files/init.sh` file, line 10.
4. Invoke `docker-compose up -d`.

## Crypter schema

After getting the PostgrSQL container running, the `crypter` database will exist, but it will not have any tables.
There are two ways to create these tables.

### Method 1 - The hacky way

Crypter.API will automatically create any missing tables in the database when it is run in development mode.
All you need to do is set the correct connection string in the project's `appsettings.json` and run the API.

A few drawbacks to this method are:
* The API does not run in development mode in production.
* The schema will be slightly different than what exists in production.

### Method 2 - The proper way

Crypter.Console has a function to create the schema.
You can use this function by building Crypter.Console then invoke via `./Crypter.Console.exe --create-schema {connection_string}`.

All of the scripts used to create the schema are stored in the Crypter.Consolr project folder.
If a new tables gets added, then we need to add a corresponding table creation script.
These scripts are the same ones used in production.

## Hangfire Notes + Steps

Crypter uses [Hangfire.io](https://www.hangfire.io/), which requires it's own database instance.
Although the SQL in `init.sh` will create a `crypter_hangfire` database, it will not contain any of the tables or sequences expected by Hangfire.

I recommend letting Hangfire take care of it's own needs.
This can be accomplished by following the rough steps below.

1. Find a Crypter project that makes use of Hangfire. E.g., Crypter.API.
2. Temporarily configure that project so it connects to the `crypter_hangfire` database using the `postgres` user.
3. Run the project.
4. Verify the `crypter_hangfire` database contains a schema named `hangfire`.
5. Verify the `hangfire` schema contains a number of tables **and** sequences.
6. Stop the project. Reconfigure the project so that it connects to `crypter_hangfire` using `cryptuser`.
7. Revoke all permissions from `crypteruser` on the tables that were created.
8. Manually grant SELECT, INSERT, UPDATE, and DELETE permissions  to those tables.
9. Run the project again and check for errors when doing things that involve Hangfire.
