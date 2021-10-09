# Deploying MySQL in a Production Environment

This document describes how to setup MySQL using docker-compose.

## Steps

1. Copy the [./Containers/MySQL](../../../Containers/MySQL) directory to the database server.
2. Review and configure the `.env` file.
3. Confirm `privileges.sql` is located within the `mysql-init-files` directory.
4. Review and configure the `privileges.sql` file *(see notes below)*.
5. Grant `read` permission to `mysql_init_files` for the `mysql` user created inside the container.
   * `chmod o+r mysql_init_files`
6. Invoke `docker-compose up -d`.

## Notes

### privileges.sql

Remote login from any IP address:

`GRANT ... TO 'user'@'*';`

Remote logins from a range of IP addresses:

`GRANT ... TO 'user'@'192.168.0.0/255.255.255.0';`
