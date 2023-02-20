#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
   CREATE DATABASE crypter_test;
   CREATE DATABASE crypter_hangfire_test;
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
   CREATE USER crypter_user WITH PASSWORD 'UNIT_TESTING_PASSWORD';
   REVOKE ALL PRIVILEGES ON DATABASE postgres FROM crypter_user;
   REVOKE ALL PRIVILEGES ON SCHEMA public FROM crypter_user;
   REVOKE CREATE ON SCHEMA public FROM PUBLIC;
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "crypter" <<-EOSQL
   REVOKE ALL PRIVILEGES ON DATABASE crypter FROM crypter_user;
   REVOKE ALL PRIVILEGES ON SCHEMA public FROM crypter_user;
   REVOKE CREATE ON SCHEMA public FROM PUBLIC;
   GRANT CONNECT ON DATABASE crypter TO crypter_user;
   ALTER DEFAULT PRIVILEGES FOR USER postgres IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO crypter_user;
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "crypter_hangfire" <<-EOSQL
   CREATE USER crypter_hangfire_user WITH PASSWORD 'UNIT_TESTING_PASSWORD';
   REVOKE ALL PRIVILEGES ON DATABASE crypter FROM crypter_hangfire_user;
   REVOKE ALL PRIVILEGES ON SCHEMA public FROM crypter_hangfire_user;

   CREATE SCHEMA IF NOT EXISTS Hangfire AUTHORIZATION crypter_hangfire_user;
   GRANT CREATE ON schema Hangfire TO crypter_hangfire_user;
EOSQL
