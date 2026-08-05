---
name: crypter-publish
description: Push a branch the pipeline built in the container to the fork and open or update its pull request. Use when a branch is ready to publish, or invoked as /crypter-publish {run-id} {branch}.
---

# Crypter publish

Take the branch the container built and put it on the fork, with a pull request open against it.

**This runs on the host.** The container holds no credential, so every authenticated GitHub
operation happens here, with yours.

Safe to run repeatedly on the same branch. Each run pushes whatever commits the container has
added and updates the existing pull request, which is what a caller looping over CI attempts
needs from it.

You are given a run id and a branch name: `/crypter-publish {run-id} {branch}`.

## 1. Fetch the branch out of the container

The branch lives in the container's clone. `git` reaches it over `docker exec`:

```bash
git -c protocol.ext.allow=user fetch \
  "ext::docker exec -i crypter-pipeline git upload-pack /work/Crypter" {branch}:{branch}
```

`protocol.ext.allow` is passed per command and stays out of your config. **If this fails, stop
and say so** — the branch is the whole deliverable.

## 2. Push to the fork

```bash
git fetch upstream
git push origin upstream/stable:refs/heads/stable
git push origin {branch}
```

The first push keeps the fork's `stable` level with the org repository, so the pull request
compares against current code.

## 3. Open or update the pull request

Where a pull request for `{branch}` is already open, the push has updated it and there is
nothing more to do. Say which one it was.

Otherwise open it against the fork, base `stable`, as a draft, using whatever GitHub access this
session has — the `gh` CLI, or the GitHub MCP server's `create_pull_request`.

Take the title and description from the report of whoever built the branch. Write the
description for the org repository's reviewers, since it carries over when the upstream pull
request is opened.

## 4. Report

The pull request URL, whether it was opened or updated, and the head commit now on it.

Checks start on the push. Watching them belongs to the caller.
