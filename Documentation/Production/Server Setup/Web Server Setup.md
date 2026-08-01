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

The secret holds `known_hosts` lines exactly as ssh writes them. The host field has to match `APPSERVER_SSH_HOST` and `APPSERVER_SSH_PORT`: a bare hostname on port 22, and `[host]:port` on any other port. A line recorded under a different name or port is never consulted, so the deploy fails as though no key had been recorded at all.

Take the key from a workstation that already connects to the server, which by this point is whichever one was used to set it up. Print the entry it trusts:

```bash
ssh-keygen -F '[<host>]:<port>' -f ~/.ssh/known_hosts
```

Drop the brackets and the port if that workstation connects over port 22. An entry that already carries the port the deploy uses can go straight into the secret, ignoring the leading comment line.

An entry recorded under any other port has to be recaptured under the right one, then checked against the entry already trusted:

```bash
ssh-keyscan -t <type> -p <port> <host> > known_hosts
ssh-keygen -lf known_hosts
ssh-keygen -F '<host>' -f ~/.ssh/known_hosts | ssh-keygen -lf -
```

The fingerprints must match. Comparing them is what makes the scan trustworthy, because `ssh-keyscan` on its own only reports whatever answers on the network. Pass `-t` for the key type that was checked, so nothing unverified lands in the secret.

Add the contents of `known_hosts` to the environment secrets as `APPSERVER_SSH_KNOWN_HOSTS`. Every environment has its own server and its own host key, so record one for each.

Rebuilding a server generates a new host key. Deploys fail with `REMOTE HOST IDENTIFICATION HAS CHANGED` until the secret is updated to match.

## Copy the .env file

Locate the `.env` file at the root of this repository, [here](../../../.env).

Copy the file to the home directory of the SSH user. For example: `/home/<github username>/crypter-web-container/.env`

Scan and update the values in the file to ensure they are correct for the environment.

## Enable linger

Enable linger to be sure the user service owned by the SSH user automatically starts after a power cycle.

`loginctl enable-linger <github username>`
