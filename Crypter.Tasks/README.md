# Deploying to a production environment

This guide is subject to change as we improve our deployment processes.

Although we may want to document various deployment methods as time goes on, the purpose of *this* guide is document the *current* deployment process. This guide should *not* reflect some hypothetical deployment in a hypothetical scenario.

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

* /var/www/Crypter.Tasks

## Update 'appsettings.json'

The `appsettings.json` can be updated at any point.  Review the file and make any necessary changes.

* Database connection string
* EncryptedFileStore setting

## Run the application

Since the current Crypter.Tasks project is a web application, the procedure to run Tasks project is very similar to running the API.

Steps:

* Copy the kestrel configuration to `/etc/systemd/system/kestrel-crypter.tasks.service`
* If this is the first time running the service, run `sudo systemctl enable kestrel-crypter.tasks.service`
* Start the service `sudo systemctl start kestrel-crypter.tasks.service`
* Check the status of the service `sudo systemctl status kestrel-crypter.tasks.service`

It appears the service starts up automatically after a reboot.
