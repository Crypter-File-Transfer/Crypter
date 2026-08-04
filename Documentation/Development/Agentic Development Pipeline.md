# Agentic Development Pipeline

The `/pipeline` skill takes a requirement from a plan to a draft pull request with green checks,
using a chain of subagents that each start with their own context. It runs inside a devcontainer
built from `.devcontainer/Dockerfile`.

Everything it does happens on **your fork**. The container's token cannot reach
`Crypter-File-Transfer/Crypter`, and the workspace is a named Docker volume rather than a bind
mount of your checkout, so the agents cannot touch uncommitted work on your machine. When the
pipeline finishes you have a fork pull request to read; opening one against the org repository is
something you do by hand afterwards.

This document covers the setup you need before the container will start.

## Configuration

`.devcontainer/.env` holds everything Compose substitutes when it creates the container. It is
tracked with empty placeholders, the same way the root `.env` is. Fill it in before the first
`up`.

| Variable | Required | Value |
|---|---|---|
| `CRYPTER_FORK` | Yes | Your fork, as `<owner>/<repo>`. Startup fails if this is the upstream repository. |
| `CRYPTER_FORK_TOKEN` | Yes | A fine-grained personal access token. Reaches the container as `GH_TOKEN`. |
| `CRYPTER_GIT_NAME` | No | Author name on the agents' commits. Defaults to `Crypter pipeline`. |
| `CRYPTER_GIT_EMAIL` | No | Author email. Defaults to `pipeline@users.noreply.github.com`. |

Leave the optional ones empty to take their defaults. The token is a live credential sitting in
a tracked file, so watch what you stage.

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

## The token

Create a fine-grained personal access token with access to **your fork only**. That restriction
is what makes the rest of the design hold: the agents push branches, open pull requests, and read
check results without any path to the org repository.

Grant it these repository permissions:

| Permission | Access | Needed for |
|---|---|---|
| Contents | Read and write | Pushing the branch |
| Pull requests | Read and write | Opening the draft pull request |
| Actions | Read | Reading check runs and failed job logs |
| Metadata | Read | Mandatory on every fine-grained token |

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
- The GitHub CLI and Claude Code

There is **no Docker in the container**, so `Crypter.Test` cannot run there — it needs
Testcontainers to start PostgreSQL. The agents build but never test locally; the test suite runs
in CI once the pull request exists, and failures come back to the implementer from there.

Two named volumes survive rebuilds: `crypter-pipeline-workspace` holds the workspace at
`/work/Crypter`, and `crypter-pipeline-claude` holds the agent's Claude Code state.

## First start

Every `up` runs `crypter-clone-fork`, which clones your fork to `/work/Crypter`, adds the org
repository as a read-only `upstream`, and fetches both. If it already finds a workspace there it
leaves it alone and only refetches, so restarting the container does not discard work in
progress.

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
