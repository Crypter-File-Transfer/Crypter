# Web Server Setup

This document describes how to manually setup a new web server for GitHub deploys.

## Install Docker

Install Docker or an equivalent.

Also install Docker Compose or an equivalent.

## Configure SSH

Create an SSH user and add corresponding details to the environment secrets within the GitHub repository.

The user will need permissions to Docker, so add the user to the `docker` group.

## Copy the .env file

Locate the `.env` file at the root of this repository, [here](../../../.env).

Copy the file to the home directory of the SSH user. For example: `/home/<github username>/crypter-web-container/.env`

Scan and update the values in the file to ensure they are correct for the environment.

## Enable linger

Enable linger to be sure the user service owned by the SSH user automatically starts after a power cycle.

`loginctl enable-linger <github username>`
