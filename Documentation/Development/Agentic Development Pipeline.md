# Agentic Development Pipeline

Two orchestrators compose a set of task skills.

| Skill | Runs | Does |
|---|---|---|
| `/crypter-change` | Host | Carries a requirement to a green draft pull request |
| `/crypter-review` | Host | Puts an existing pull request through the reviewer lenses |
| `/crypter-plan` | Host | Drafts the plan interactively, with the web, your tooling and you available to it |
| `/crypter-implement` | Container | Builds the plan into commits on a new branch |
| `/crypter-examine` | Container | Reviews a diff for plan adherence and code quality |
| `/crypter-remediate` | Container | Applies triaged findings or a CI failure to an existing branch |
| `/crypter-publish` | Host | Pushes the branch and opens or updates the pull request |

The task skills stand alone. `/crypter-plan` is worth running on its own when you want a plan
and nothing else, and `/crypter-publish` is safe to run repeatedly, which is how the CI loop
uses it.

**The container holds no credential.** Its workspace is an anonymous clone of the org
repository with one remote, `upstream`, which has no push url, so the agents read public code
and commit locally. Every authenticated GitHub operation happens on the host with your own
access. The workspace is a named Docker volume rather than a bind mount of your checkout, so
the agents cannot touch uncommitted work on your machine.

When `/crypter-change` finishes you have a fork pull request to read; opening one against the org
repository is something you do by hand afterwards.

This document covers the setup you need before the container will start.

## The two mounts

Everything crossing the container boundary goes through one of two directories, both gitignored
and both on your disk:

| Host | Container | Direction | Holds |
|---|---|---|---|
| `.claude/plans/{run-id}` | `/plans` | Read-only | `plan.md` |
| `.claude/runs/{run-id}` | `/runs` | Writable | `conformance.md`, `findings/{lens}.md`, `triage.md`, `ci-{n}.md` |

The plan goes in and cannot be rewritten by the agents. Findings come back out as files you can
open, grep and keep, rather than as text in a transcript, and each is written by the agent that
found it. `triage.md` is what `/crypter-change` decided to act on, and reading it is how you
check that judgement.

The container's `agent` user is uid 1001, because the base image already has a user on 1000. A
bind mount keeps host ownership, so the orchestrators create every directory under `.claude/runs`
themselves and give it mode 777. Directories made on the host stay deletable from the host; a
directory the container creates is one you need `docker exec` to remove.

The branch itself travels differently. It never passes through a mount:

```bash
git -c protocol.ext.allow=user fetch \
  "ext::docker exec -i crypter-pipeline git upload-pack /work/Crypter" {branch}:{branch}
```

`protocol.ext.allow` is passed per command, so it stays out of your git config.

A container created before these mounts existed picks them up on
`docker compose -f .devcontainer/docker-compose.yml up -d --force-recreate`.

## Running a change

```bash
/crypter-change "<requirement>"
```

It plans, stops for your approval, then builds, examines, triages, remediates, publishes, and
holds the pull request against CI for at most three fix attempts. The approval is the only stop.

`/crypter-review {pr-number}` is the other entry point. It fetches a pull request's head into the
container, runs the lenses against it with no plan to audit, and reports. It posts nothing to
GitHub.

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

## Enable Actions on your fork

GitHub disables workflows on new forks. Until you turn them on, pushing a branch runs nothing,
and `/crypter-change` stops at the CI stage reporting that no run ever appeared.

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
