# Agentic Development Pipeline

A change goes through three skills:

| Skill | Runs | Does |
|---|---|---|
| `/crypter-plan-author` | Host | Drafts the plan interactively, with the web, your tooling and you available to it |
| `/pipeline` | Container | Implements the plan, reviews it, and leaves a branch |
| `/crypter-publish` | Host | Pushes the branch, opens the pull request, holds it against CI |

**The container holds no credential.** Its workspace is an anonymous clone of the org
repository with one remote, `upstream`, which has no push url, so the agents read public code
and commit locally. Every authenticated GitHub operation happens on the host with your own
access. The workspace is a named Docker volume rather than a bind mount of your checkout, so
the agents cannot touch uncommitted work on your machine.

When `/crypter-publish` finishes you have a fork pull request to read; opening one against the
org repository is something you do by hand afterwards.

This document covers the setup you need before the container will start.

## Planning and the plans mount

`/crypter-plan-author` writes to `.claude/plans/{run-id}/plan.md` on your host, which is
gitignored. Compose mounts `.claude/plans` read-only at `/plans` in the container, so the
pipeline reads the plan where you wrote it and the agents write their run state to the workspace
instead.

You approve the plan in that host session. It then starts the pipeline itself:

```bash
docker exec -w /work/Crypter crypter-pipeline \
  claude --dangerously-skip-permissions -p "/pipeline {run-id} {branch}"
```

A container created before the mount existed picks it up on
`docker compose -f .devcontainer/docker-compose.yml up -d --force-recreate`.

## Configuration

`.devcontainer/.env` holds everything Compose substitutes when it creates the container. It is
ignored by git. Copy the template and fill it in before the first `up`.

```bash
cp .devcontainer/.env.example .devcontainer/.env
```

| Variable | Value |
|---|---|
| `CRYPTER_GIT_NAME` | Author name on the agents' commits. |
| `CRYPTER_GIT_EMAIL` | Author email on the agents' commits. |

Both are required. Leaving one empty fails the container's startup script with a message naming
the variable.

## Launching the container

The container is a Compose service in `.devcontainer/docker-compose.yml`. That is a separate
Compose project from the application stack at the repository root, so `docker compose up` and
`docker compose down` there never touch it, and the two share no network.

```bash
docker compose -f .devcontainer/docker-compose.yml up -d
docker compose -f .devcontainer/docker-compose.yml exec -w /work/Crypter pipeline bash
```

Swap `up -d` for `down` to stop it. The named volumes outlive the container, so the next `up`
reuses the workspace and your Claude Code credentials.

## Publishing

`/crypter-publish {run-id} {branch}` reaches into the container for the branch, using git's
`ext` transport over `docker exec`:

```bash
git -c protocol.ext.allow=user fetch \
  "ext::docker exec -i crypter-pipeline git upload-pack /work/Crypter" {branch}:{branch}
```

`protocol.ext.allow` is passed per command, so it stays out of your git config. From there the
host pushes the branch to your fork, opens the draft pull request, and runs the CI loop with
your own GitHub access — the `gh` CLI or the GitHub MCP server. Three fix attempts is the
ceiling; `/crypter-publish` writes each failure to `.claude/plans/{run-id}/ci-{n}.md` and runs
`/pipeline-fix` in the container to address it.

## Enable Actions on your fork

GitHub disables workflows on new forks. Until you turn them on, pushing a branch runs nothing,
and the pipeline stops at the CI stage reporting that no run ever appeared.

Open the **Actions** tab on your fork and use the button confirming you want to run workflows.
You only do this once.

## What is in the container

The image is published by the org at `ghcr.io/crypter-file-transfer/crypter-devcontainer`, and
the container pulls it for you. There is nothing to build unless you are changing the image
itself.

Built on `mcr.microsoft.com/dotnet/sdk:10.0`, running as an unprivileged user named `agent`
because Claude Code refuses `--dangerously-skip-permissions` as root:

- The .NET 10 SDK, the `wasm-tools` workload, and `dotnet-ef`
- Node 22 and pnpm 11.18.0, which `Crypter.Web`'s PreBuild target needs
- Claude Code

There is **no Docker in the container**, so `Crypter.Test` cannot run there — it needs
Testcontainers to start PostgreSQL. The agents build but never test locally; the test suite runs
in CI once the pull request exists, and failures come back to the implementer from there.

Two named volumes survive rebuilds: `crypter-pipeline-workspace` holds the workspace at
`/work/Crypter`, and `crypter-pipeline-claude` holds the agent's Claude Code state.

## First start

Every `up` runs `crypter-clone-upstream`, which clones the org repository to `/work/Crypter` as
the `upstream` remote, clears that remote's push url, and fetches. If it already finds a
workspace there it leaves it alone and only refetches, so restarting the container does not
discard work in progress.

To start over from nothing, take the container down and remove the volumes:

```bash
docker compose -f .devcontainer/docker-compose.yml down
docker volume rm crypter-pipeline-workspace crypter-pipeline-claude
```

## Authenticate Claude Code

The image ships Claude Code but no credentials. Run `claude` once inside the container and
follow the login prompt. The container has no browser, so the flow gives you a URL to open on
your host and a code to paste back.

Credentials live in `/home/agent/.claude`, which is the `crypter-pipeline-claude` volume, so
they survive container rebuilds. You only do this again after removing that volume.

Run the agents with `--dangerously-skip-permissions`. A pipeline that stops to approve every
file write is not a pipeline, and the fork-scoped token is what bounds the blast radius rather
than the permission prompts. That flag is also why the container runs as the unprivileged
`agent` user; Claude Code refuses it as root.

## Changing the image

Only needed if your change requires a different image — a new tool the agents need, a runtime
version bump. Otherwise skip this; the published image is what the container runs.

Build your change locally to try it:

```bash
docker compose -f .devcontainer/docker-compose.yml build
docker compose -f .devcontainer/docker-compose.yml up -d
```

Open a pull request for `.devcontainer/` once it works. `pr-build-devcontainer` builds the image
on the pull request, and merging to `stable` runs
`.github/workflows/build-and-push-devcontainer.yml`, which pushes to
`ghcr.io/crypter-file-transfer/crypter-devcontainer`. That job runs in the `devcontainer`
environment, so it waits for a reviewer to approve it before anything is published.
