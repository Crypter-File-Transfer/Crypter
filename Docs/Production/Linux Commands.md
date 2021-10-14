# Linux Commands

This document contains various commands used on the production, Linux server.

## Nginx

Stop the Nginx web server before updating any of the Crypter web applications.

Restart the Nginx web server after updating the Nginx configuration.

#### Commands

* /etc/init.d/nginx start
* /etc/init.d/nginx stop
* /etc/init.d/nginx restart

## Kestrel

Stop the Kestrel web server before updating `Crypter.API`.

#### Commands

* systemctl start kestrel-crypter.api.service
* systemctl stop kestrel-crypter.api.service
* systemctl restart kestrel-crypter.api.service

## Cron

A cron job exists to periodically run `Crypter.Console` in order to delete expired transfers.

Run `$ crontab -e` to view and edit the cron job.

## PostgreSQL

Run `$ psql --username {user} --password --dbname {dbname}` to login to the PostgreSQL server.  You will be prompted for a password.
