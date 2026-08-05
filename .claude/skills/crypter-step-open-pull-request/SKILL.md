---
name: crypter-step-open-pull-request
description: Push a branch the pipeline built in the container to the repository and open or update its draft pull request. Invoked as /crypter-step-open-pull-request {run-id} {branch} by the crypter-change and crypter-triage-review skills.
---

# Crypter step open pull request

Take the branch the container built and put it on the repository, with a draft pull request open
against it.

Safe to run repeatedly on the same branch. Each run pushes whatever commits the container has
added and updates the existing pull request.

You are given a run id and a branch name: `/crypter-step-open-pull-request {run-id} {branch}`.

## 1. Fetch the branch out of the container

The branch lives in the container's clone. `git` reaches it over `docker exec`:

```bash
git -c protocol.ext.allow=user fetch \
  "ext::docker exec -i crypter-pipeline git upload-pack /work/Crypter" {branch}:{branch}
```

`protocol.ext.allow` is passed per command and stays out of your config. **If this fails, stop
and say so** — the branch is the whole deliverable.

## 2. Push to the repository

`origin` is the org repository, the same one the container cloned and the same one the pull
request opens against. Confirm that before pushing anything:

```bash
git remote get-url origin
```

**If it is not `Crypter-File-Transfer/Crypter`, stop and say so.** A checkout wired up
differently — a fork on `origin`, or the org on some other remote — pushes the branch somewhere
the pull request will not find it.

```bash
git fetch origin
git push origin {branch}
```

## 3. Open or update the pull request

Where a pull request for `{branch}` is already open, the push has updated it and there is
nothing more to do. Say which one it was.

Otherwise open it against `Crypter-File-Transfer/Crypter`, base `stable`, as a draft, using
whatever GitHub access this session has — the `gh` CLI, or the GitHub MCP server's
`create_pull_request`.

It stays a draft. Taking it out of draft is the user's.

Take the title and description from the report of whoever built the branch. Write the
description for the reviewers who will read it on that pull request.

## 4. Report

The pull request URL, whether it was opened or updated, and the head commit now on it.

Checks start on the push. Watching them belongs to the caller.
