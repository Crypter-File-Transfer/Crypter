# Deploying Crypter.Web to a production environment

This guide is subject to change as we improve our deployment processes.

Although we may want to document various deployment methods as time goes on, the purpose of this guide is document the current deployment process. This guide should not reflect some hypothetical deployment in a hypothetical scenario.

## Update 'appsettings.json'
Mke changes to the `appsettings.json` file **before** publishing the project!

## Publish the project

Our current publish consists of making a 'release' build and saving the output to a folder.

A publish 'profile' is checked in to the repo under `.\Properties\PublishProfiles\FolderProfile.pubxml`. This profile should work fine on all our development machines without modification.

Steps:

1. Right-click on the project in Visual Studio
2. Select `Publish`
3. Verify the `FolderProfile.pubxml` is selected
4. Click `Publish`

## Copy the published output to the server

Take the published output from `.\Crypter.Web\bin\Release\net5.0\publish` from your development machine.

Copy the output to `/var/www/Crypter.Web` on the web server.

## Configure Nginx

The production server currently uses Nginx to serve Crypter.Web as a **static** website. This deviates from what we may be doing on our developer machines.

The Nginx configuration is checked in to source control at [..\Configurations\nginx_config](..\Configurations\nginx_config).

Steps:

1. Copy the `nginx_config` to `/etc/nginx/sites-available`
2. Rename the `nginx_config` file to `crypter`
3. Create a symlink to `/etc/nginx/sites-enabled`
   * `sudo ln -s /etc/nginx/sites-available/crypter /etc/nginx/sites-enabled/crypter`
4. Remove the `default` config from `sites-enabled`, if it exists. This is usually just a symlink to a file in `sites-available`
   * `sudo rm /etc/nginx/sites-enabled/default`
5. Test the configuration
   * `sudo nginx -t`

## Notes

This guide does not cover Let's Encrypt or Certbot, which are required to get host Crypter and does have an impact on the Nginx setup.
