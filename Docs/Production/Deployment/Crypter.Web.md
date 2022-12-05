# Deploying Crypter.Web to a production environment

This guide is subject to change as we improve our deployment processes.

Although we may want to document various deployment methods as time goes on, the purpose of this guide is document the current deployment process. This guide should not reflect some hypothetical deployment in a hypothetical scenario.

## Update 'appsettings.json'

The `appsettings.json` file for Crypter.Web exists under `wwwroot`.
This file is sent to the browser with every request.

## Publish the project

Our current publish consists of making a 'release' build and saving the output to a folder.

A publish 'profile' is checked in to the repo under `.\Properties\PublishProfiles\FolderProfile.pubxml`. This profile should work fine on all our development machines without modification.

Steps:

1. Right-click on the project in Visual Studio
2. Select `Publish`
3. Verify the `FolderProfile.pubxml` is selected
4. Click `Publish`

## Copy the published output to the server

Take the published output from `.\Crypter.Web\bin\Release\net6.0\publish\browser-wasm` from your development machine.

Copy the output to `/var/www/Crypter.Web` on the web server.

## Configure Nginx

The production server currently uses Nginx to serve Crypter.Web as a **static** website. This deviates from what we may be doing on our developer machines.

The primary Nginx configuration for the web server is checked in to source control at [..\Configurations\nginx.conf](..\Configurations\nginx.conf).

The Nginx configuration for the Crypter web app is checked in at [..\Configurations\nginx_crypter_config](..\Configurations\nginx_crypter_config).
This includes configuration for both Crypter.API and Crypter.Web.

Steps for the primary Nginx configuration:

1. Get the version of Nginx currently installed
   * `nginx -v`
2. Download the corresponding source code from the Nginx website
   * `wget https://nginx.org/download/nginx-1.18.0.tar.gz`
3. Extract the source code
   * `tar xzf nginx-1.18.0.tar.gz`
4. Clone the `ngx_brotli` module source code
   * `git clone --recursive https://github.com/google/ngx_brotli`
5. Configure the Brotli module
   * `cd nginx-1.18.0`
   * `sudo ./configure --with-compat --add-dynamic-module=../ngx_brotli`
   * Note, you may need to install the `build-essential`, `libpcre3-dev`, and `zlib1g-dev` build and fully configure the module
6. Compile the Brotli modules from within the `nginx-1.18.0` directory
   * `sudo make modules`
7. Copy the compiled files to the Nginx `modules` folder
   * `cd nginx-1.18.0/objs`
   * `sudo cp ngx_http_brotli*.so /usr/share/nginx/modules`
8. The Nginx `modules` folder should contain the following files:
   * `ngx_http_brotli_filter_module.so`
   * `ngx_http_brotli_static_module.so`
9. Copy the settings in `nginx.conf` to `/etc/nginx/nginx.conf`.

Steps for configuring Nginx for Crypter:

1. Place the `nginx_crypter_config` file in `/etc/nginx/sites-available`
2. Rename the `nginx_crypter_config` file to `crypter`
3. Create symlinks to `/etc/nginx/sites-enabled`
   * `sudo ln -s /etc/nginx/sites-available/crypter /etc/nginx/sites-enabled/crypter`
4. Remove the `default` config from `sites-enabled`, if it exists. This is usually just a symlink to a file in `sites-available`
   * `sudo rm /etc/nginx/sites-enabled/default`
5. Test the configuration
   * `sudo nginx -t`

## Notes

This guide does not cover Let's Encrypt or Certbot, which are required to host Crypter and does have an impact on the Nginx setup.
