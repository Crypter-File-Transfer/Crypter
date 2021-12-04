# Deploying PostgreSQL in a Production Environment

This document describes how to setup PostgreSQL using docker-compose.

## Steps

1. Copy the [./Containers/PostgreSQL](../../../Containers/PostgreSQL) directory to the database server.
2. Review and configure the `.env` file.
3. Modify the user password in the `./postgres-init-files/init.sh` file, line 10.
4. Invoke `docker-compose up -d`.

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
