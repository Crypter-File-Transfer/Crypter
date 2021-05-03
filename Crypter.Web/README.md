# Deploying to a production environment

This guide is subject to change as we improve our deployment processes.

Although we may want to document various deployment methods as time goes on, the purpose of *this* guide is document the *current* deployment process. This guide should *not* reflect some hypothetical deployment in a hypothetical scenario.

## Update 'appsettings.json'
Any changes that need to be made in the `appsettings.json` file need to be made *before* publishing the project.

Steps:

* Make any changes to `appsettings.json` now

## Publish the project

Our current publish consists of making a 'release' build and saving the output to a folder.

A publish 'profile' is checked in to the repo under `./Properties/PublishProfiles/FolderProfile.pubxml`. This profile should work fine on all our development machines without modification.

Steps:

* Right-click on the project in Visual Studio
* Select `Publish`
* Verify the `FolderProfile.pubxml` is selected
* Click `Publish`

## Copy the published output to the server

The publish output needs to be copied to the production server.

Steps:

* /var/www/Crypter.Web/wwwroot
* /var/www/Crypter.Web/web.config

## Configure Nginx

The production server currently uses Nginx to serve Crypter.Web as a **static** website. This deviates from what we may be doing on our developer machines.

The Nginx configuration is located in this repository at `.\Configurations\Production\nginx_config`.

Steps:

* Copy the `nginx_config` to `/etc/nginx/sites-available/crypter`

**Note** `./crypter` is a file.  It is not a directory.
