# Deploying PostgreSQL in a Production Environment

This document describes how to setup PostgreSQL using docker-compose.

## Steps

1. Copy the [./Containers/PostgreSQL](../../../Containers/PostgreSQL) directory to the database server.
2. Review and configure the `.env` file.
3. Modify the user password in the `./postgres-init-files/init.sh` file.
4. Invoke `docker-compose up -d`.
