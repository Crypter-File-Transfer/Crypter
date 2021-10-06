# Deploying Crypter.API to a production environment

This guide is subject to change as we improve our deployment processes.

Although we may want to document various deployment methods as time goes on, the purpose of this guide is document the current deployment process. This guide should not reflect some hypothetical deployment in a hypothetical scenario.

## Publish the project

Our current publish consists of making a 'release' build and saving the output to a folder.

A publish 'profile' is checked in to the repo under `.\Properties\PublishProfiles\FolderProfile.pubxml`. This profile should work fine on all our development machines without modification.

Steps:

1. Right-click on the project in Visual Studio
2. Select `Publish`
3. Verify the `FolderProfile.pubxml` is selected
4. Click `Publish`

## Copy the published output to the server

Take the published output from `.\Crypter.API\bin\Release\net5.0\linux-x64` from your development machine.

Copy the output to `/var/www/Crypter.API` on the web server.

## Update 'appsettings.json'

The `appsettings.json` can be updated at any point for Crypter.API.  Review the file and make any necessary changes.

* Database connection string
* FileStore settings

## Run the application

Unlike the Web application, this is a proper application that needs to be "run".

Steps:

1. Copy the kestrel configuration to `/etc/systemd/system/kestrel-crypter.api.service`
2. If this is the first time running the service, run `sudo systemctl enable kestrel-crypter.api.service`
3. Start the service `sudo systemctl start kestrel-crypter.api.service`
4. Check the status of the service `sudo systemctl status kestrel-crypter.api.service`

The service will automatically start up after a reboot.
