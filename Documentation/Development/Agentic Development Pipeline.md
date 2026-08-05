# Agentic Development Pipeline

Three orchestrators compose a set of task skills. You invoke an orchestrator in your own session,
and it invokes the rest.

| Orchestrator | Does |
|---|---|
| `/crypter-change "<requirement>"` | Carries a requirement to a green draft pull request |
| `/crypter-review {pr-number}` | Puts an existing pull request through the reviewer lenses |
| `/crypter-triage-review {pr-number}` | Rules on the findings left on a pull request and fixes the ones that hold |

| Task skill | Executes in | Does |
|---|---|---|
| `/crypter-step-plan` | Your session | Drafts the plan interactively, with the web, your tooling and you available to it |
| `/crypter-devcontainer-implement` | Container | Builds the plan into commits on a new branch |
| `/crypter-devcontainer-examine` | Container | Reviews a diff for plan adherence and code quality |
| `/crypter-devcontainer-verify` | Container | Rules on each finding in a report against the code |
| `/crypter-devcontainer-remediate` | Container | Applies triaged findings or a CI failure to an existing branch |
| `/crypter-step-open-pull-request` | Your session | Pushes the branch and opens or updates the draft pull request |

Every skill that reads or writes code runs in the container, against the container's own clone.
Your session plans, decides what to act on, and talks to GitHub. `/crypter-review` reviews
nothing itself: it fetches the pull request into the container and runs
`/crypter-devcontainer-examine` there.

Both prefixes say the same thing: an orchestrator invokes this, you do not. `crypter-step-` runs
in your session, and `crypter-devcontainer-` runs in the container, which expects `/work/Crypter`,
`/plans` and `/runs` — none of which your session has. The three skills without a prefix are the
ones to invoke.

`/crypter-step-plan` is the one worth borrowing when you want a plan and nothing else, and
`/crypter-step-open-pull-request` is safe to run repeatedly, which is how the CI loop uses it.

**Run the orchestrators from the root of your main checkout.** The container's mounts are
relative to `.devcontainer/`, so `.claude/plans` and `.claude/runs` resolve against that one
directory. Started from a worktree, a run writes its plan somewhere the container cannot read.

**The container holds no GitHub credential.** Its workspace is an anonymous clone of the org
repository with one remote, `upstream`, which has no push url, so the agents read public code
and commit locally. Every authenticated GitHub operation happens in your session with your own
access, and `/crypter-change` pushes and re-pushes without stopping to ask. The workspace is a
named Docker volume rather than a bind mount of your checkout, so the agents cannot touch
uncommitted work on your machine.

The container does hold your Claude Code credential, in the `crypter-pipeline-claude` volume,
and its network egress is open. Treat it as a trust boundary rather than a sandbox.

The branch is pushed to the org repository and the pull request opens against it, base `stable`,
the same route a branch of your own takes. `/crypter-change` leaves you a draft pull request to
read.

The org repository therefore has two names in this pipeline. Your session reaches it as `origin`,
the remote your checkout already has. The container reaches it as `upstream`, the name its clone
gives the one remote it has, chosen so that a remote with no push url reads as one. Host-side
skills say `origin` and container-side skills say `upstream`; both mean
`Crypter-File-Transfer/Crypter`.

This document covers the setup you need before the container will start.

## The two mounts

Everything crossing the container boundary goes through one of two directories, both gitignored
and both on your disk:

| Host | Container | Direction | Holds |
|---|---|---|---|
| `.claude/plans` | `/plans` | Read-only | `{run-id}/plan.md` |
| `.claude/runs` | `/runs` | Writable | `{run-id}/conformance.md`, `{run-id}/findings/{lens}.md`, `{run-id}/review.md`, `{run-id}/verification/{id}.md`, `{run-id}/triage.md`, `{run-id}/ci-{n}.md` |

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

It plans, stops for your approval, then builds, examines, triages, remediates, opens the draft
pull request, and holds it against CI for at most three fix attempts. The approval is the only
stop, and the pull request stays a draft until you take it out of one.

`/crypter-review {pr-number}` is the second entry point. It fetches a pull request's head into
the container, runs the lenses against it with no plan to audit, triages what they raise, and
posts one review that comments. It never approves and never requests changes.

`/crypter-triage-review {pr-number}` is the third. It reads the findings already on a pull
request, whoever left them, and gives one verifier per finding a worktree and nothing else to
judge it by. A finding that does not survive that gets a reply on its thread saying what the
code does instead. A finding that does becomes a commit, where the head branch is one you can
push to. Nothing is fixed on the strength of the finding alone.

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
mkdir -p .claude/plans .claude/runs
docker compose -f .devcontainer/docker-compose.yml up -d
docker compose -f .devcontainer/docker-compose.yml exec -w /work/Crypter pipeline bash
```

Create the two mount sources first. They are gitignored, so a fresh clone has neither, and
Docker creates a missing bind-mount source as root — which the orchestrators then cannot write
into.

Swap `up -d` for `down` to stop it. The named volumes outlive the container, so the next `up`
reuses the workspace and your Claude Code credentials.

## What is in the container

The image is published by the org at `ghcr.io/crypter-file-transfer/crypter-devcontainer`, and
the container pulls it for you. There is nothing to build unless you are changing the image
itself.

Built on `mcr.microsoft.com/dotnet/sdk:10.0`, running as an unprivileged user named `agent`
rather than as root:

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

Run the agents with `--permission-mode auto`. They work unattended, so a prompt they cannot
answer is a run that stalls. What bounds the blast radius is the container itself: a workspace
in a named volume, a remote with no push url, and no GitHub credential to push with.

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
