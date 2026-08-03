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

## Host environment variables

`devcontainer.json` passes these through from your machine. Set them wherever your shell reads
its environment from, before launching the container.

| Variable | Required | Value |
|---|---|---|
| `CRYPTER_FORK` | Yes | Your fork, as `<owner>/<repo>`. Startup fails if this is the upstream repository. |
| `CRYPTER_FORK_TOKEN` | Yes | A fine-grained personal access token. Reaches the container as `GH_TOKEN`. |
| `CRYPTER_DEVCONTAINER_OWNER` | No | Only if you build your own image. See below. Defaults to `crypter-file-transfer`. |
| `CRYPTER_GIT_NAME` | No | Author name on the agents' commits. Defaults to `Crypter pipeline`. |
| `CRYPTER_GIT_EMAIL` | No | Author email. Defaults to `pipeline@users.noreply.github.com`. |

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

Two named volumes survive rebuilds: `crypter-pipeline-workspace` holds the clone at
`/work/Crypter`, and `crypter-pipeline-claude` holds the agent's Claude Code state.

## First start

On creation the container clones your fork to `/work/Crypter`, adds the org repository as a
read-only `upstream`, and fetches both. If it already finds a clone there it leaves it alone, so
rebuilding the container does not discard work in progress.

To start over from nothing, remove the volumes and reopen the container:

```bash
docker volume rm crypter-pipeline-workspace crypter-pipeline-claude
```

## Building your own image

Only needed if your change requires a different image — a new tool the agents need, a runtime
version bump. Otherwise skip this; the org's published image is the default.

`.github/workflows/build-and-push-devcontainer.yml` builds and pushes to
`ghcr.io/<repository owner>/<DEVCONTAINER_IMAGE_NAME>`, on pushes to `stable` touching
`.devcontainer/` and on manual dispatch. To publish from your fork:

1. Set the repository variable `DEVCONTAINER_IMAGE_NAME` to `crypter-devcontainer`, under
   **Settings → Secrets and variables → Actions → Variables**. It is a variable, not a secret.
   Unset, the workflow builds a malformed image reference and tagging fails.
2. Run the workflow from the Actions tab.
3. Make the resulting package public in its package settings. Packages are private when first
   pushed, and a private one needs a `docker login ghcr.io` before the container can pull it.
4. Set `CRYPTER_DEVCONTAINER_OWNER` on your host to your GitHub account name, lowercase, and
   rebuild the container.

Changes to the image belong upstream once they work. Open a pull request for `.devcontainer/`
against the org repository and unset `CRYPTER_DEVCONTAINER_OWNER` when it merges.
