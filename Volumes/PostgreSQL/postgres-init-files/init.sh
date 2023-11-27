#!/bin/bash
set -e

# Create databases
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
   CREATE DATABASE crypter;
   CREATE DATABASE crypter_hangfire;
   
   REVOKE CONNECT ON DATABASE postgres FROM PUBLIC;
   REVOKE CONNECT ON DATABASE crypter FROM PUBLIC;
   REVOKE CONNECT ON DATABASE crypter_hangfire FROM PUBLIC;
EOSQL

# Create the "citext" extension on the "crypter" database
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "crypter" <<-EOSQL
  CREATE EXTENSION IF NOT EXISTS citext;
EOSQL

# Create crypter_user
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "crypter" <<-EOSQL
   CREATE USER crypter_user WITH PASSWORD '$POSTGRES_C_PASSWORD';

   REVOKE ALL PRIVILEGES ON DATABASE crypter_hangfire FROM crypter_user;
   REVOKE CONNECT ON DATABASE crypter_hangfire FROM crypter_user;

   GRANT CONNECT ON DATABASE crypter TO crypter_user;
   CREATE SCHEMA IF NOT EXISTS crypter AUTHORIZATION crypter_user;
   GRANT CREATE ON DATABASE crypter TO crypter_user;
   GRANT CREATE ON SCHEMA public TO crypter_user;
   GRANT CREATE ON SCHEMA crypter TO crypter_user;
   GRANT USAGE ON SCHEMA public TO crypter_user;
   GRANT USAGE ON SCHEMA crypter TO crypter_user;
EOSQL

# Create crypter_hangfire_user
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "crypter_hangfire" <<-EOSQL
   CREATE USER crypter_hangfire_user WITH PASSWORD '$POSTGRES_HF_PASSWORD';

   REVOKE ALL PRIVILEGES ON DATABASE crypter FROM crypter_hangfire_user;
   REVOKE CONNECT ON DATABASE crypter FROM crypter_hangfire_user;

   GRANT CONNECT ON DATABASE crypter_hangfire TO crypter_hangfire_user;
   CREATE SCHEMA IF NOT EXISTS hangfire AUTHORIZATION crypter_hangfire_user;
   GRANT CREATE ON DATABASE crypter_hangfire TO crypter_hangfire_user;
   GRANT CREATE ON SCHEMA public TO crypter_hangfire_user;
   GRANT CREATE ON SCHEMA hangfire TO crypter_hangfire_user;
   GRANT USAGE ON SCHEMA public TO crypter_hangfire_user;
   GRANT USAGE ON SCHEMA hangfire TO crypter_hangfire_user;
EOSQL
