# Linux Commands

This document contains various commands used on the production, Linux server.

## Nginx

Stop the Nginx web server before updating any of the Crypter web applications.

Restart the Nginx web server after updating the Nginx configuration.

#### Commands

Check the status
* systemctl status nginx

Lifecycle
* systemctl start nginx
* systemctl stop nginx
* systemctl reload nginx
* systemctl restart nginx

Enable or disable
* systemctl enable nginx
* systemctl disable nginx

## Kestrel

Stop the Kestrel web server before updating `Crypter.API`.

#### Commands

* systemctl start kestrel-crypter.api.service
* systemctl stop kestrel-crypter.api.service
* systemctl restart kestrel-crypter.api.service

## PostgreSQL

Run `$ psql --username {user} --password --dbname {dbname}` to login to the PostgreSQL server.  You will be prompted for a password.
