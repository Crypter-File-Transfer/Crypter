# Web Server Setup

This document describes how to manually setup a new web server for GitHub deploys.

## Install Docker

Install Docker or an equivalent.

Also install Docker Compose or an equivalent.

## Configure SSH

Create an SSH user and add corresponding details to the environment secrets within the GitHub repository.

The user will need permissions to Docker, so add the user to the `docker` group.

## Record the host key

The deploy workflow verifies the host key of the server it connects to, so record that key while the server is being set up.

On the server, print the fingerprint of each host key:

`ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub`

From a workstation, capture the same keys in `known_hosts` format and print their fingerprints:

```bash
ssh-keyscan -p <port> <host> > known_hosts
ssh-keygen -lf known_hosts
```

The fingerprints must match. Comparing them is what makes the captured key trustworthy, because `ssh-keyscan` on its own only reports whatever answers on the network.

Add the contents of `known_hosts` to the environment secrets as `APPSERVER_SSH_KNOWN_HOSTS`. Every environment has its own server and its own host key, so record one for each.

Rebuilding a server generates a new host key. Deploys fail with `REMOTE HOST IDENTIFICATION HAS CHANGED` until the secret is updated to match.

## Copy the .env file

Locate the `.env` file at the root of this repository, [here](../../../.env).

Copy the file to the home directory of the SSH user. For example: `/home/<github username>/crypter-web-container/.env`

Scan and update the values in the file to ensure they are correct for the environment.

## Enable linger

Enable linger to be sure the user service owned by the SSH user automatically starts after a power cycle.

`loginctl enable-linger <github username>`
