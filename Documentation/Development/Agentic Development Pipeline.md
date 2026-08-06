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
Your session plans, decides what to act on, and talks to GitHub. It does not read code to form a
view on it — reviewing a diff and ruling on a finding are both judgements made in the container,
by an agent with the code in front of it. `/crypter-review` reviews nothing itself and settles
nothing itself: it fetches the pull request into the container, runs
`/crypter-devcontainer-examine` there, has `/crypter-devcontainer-verify` rule on what came back,
and carries the survivors to GitHub.

Both prefixes say the same thing: an orchestrator invokes this, you do not. `crypter-step-` runs
in your session, and `crypter-devcontainer-` runs in the container, which expects a workspace,
`/plans` and `/runs` — none of which your session has. The three skills without a prefix are the
ones to invoke.

`/crypter-step-plan` is the one worth borrowing when you want a plan and nothing else, and
`/crypter-step-open-pull-request` is safe to run repeatedly, which is how the CI loop uses it.

**Run the orchestrators from the root of your main checkout.** The container's mounts are
relative to `.devcontainer/`, so `.claude/plans` and `.claude/runs` resolve against that one
directory. Started from a worktree, a run writes its plan somewhere the container cannot read.

**The container holds no GitHub credential and no network remote.** Every authenticated GitHub
operation happens in your session with your own access, and `/crypter-change` pushes and
re-pushes without stopping to ask.

The branch is pushed to the org repository and the pull request opens against it, base `stable`,
the same route a branch of your own takes. `/crypter-change` leaves you a draft pull request to
read.

The container does hold your Claude Code credential, in the `crypter-pipeline-claude` volume,
and its network egress is open. Treat it as a trust boundary rather than a sandbox.

## Workspaces

The agents build and review in a **workspace**: a clone of your repository at `/work/{run-id}`,
made when a run starts and deleted when it ends. Nothing that holds a copy of the code outlives
the run that made it, so there is no second checkout drifting away from yours.

Workspaces are cloned from `/host-git`, a read-only mount of your repository's `.git`. Read-only
is what makes this safe to share: the container reads committed history and cannot move a ref,
add an object, or touch anything in your repository. Your working tree is not mounted at all, so
uncommitted work is invisible in there and cannot reach a branch.

The orchestrator owns the lifecycle. It creates the workspace in its setup and removes it when
the run ends; the container skills use it and never create or destroy one.

```bash
docker exec crypter-pipeline crypter-workspace create {run-id} [--base {branch}] [refspec]
docker exec crypter-pipeline crypter-workspace remove {run-id}
```

The org repository is `origin` on both sides. A clone would otherwise map your local branches
into the workspace's `origin/*`, so the create step points the remote at your remote-tracking
refs instead, and `origin/stable` in a workspace means what it means in your checkout. Fetch
before creating a workspace, or the run starts on a stale base.

`--base` is the branch the run is built or reviewed against, `stable` when it is not given. A
pull request states its own base, and a release states `main`, so the review skills pass what
they read rather than assuming.

This document covers the setup you need before the container will start.

## The mounts

Everything crossing the container boundary goes through one of these:

| Host | Container | Direction | Holds |
|---|---|---|---|
| `.git` | `/host-git` | Read-only | Your committed history, which workspaces are cloned from |
| `.claude/plans` | `/plans` | Read-only | `{run-id}/plan.md` |
| `.claude/runs` | `/runs` | Writable | `{run-id}/conformance.md`, `{run-id}/findings/{lens}.md`, `{run-id}/review.md`, `{run-id}/verification/{id}.md`, `{run-id}/triage.md`, `{run-id}/ci-{n}.md` |

`.claude/plans` and `.claude/runs` are gitignored and live on your disk. Only `/runs` is
writable; the other two the container can read and nothing more.

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
  "ext::docker exec -i crypter-pipeline git upload-pack /work/{run-id}" {branch}:{branch}
```

`protocol.ext.allow` is passed per command, so it stays out of your git config.

Because the mounts are relative paths in the Compose file, they resolve against the checkout you
launch from, and a container is stuck with whatever they resolved to when it was created. Run
the pipeline from a second checkout and the container it finds by name is the first one's:
`/runs` writes land under a repository you are not looking at, and `/host-git` clones a history
that is not the one you are reviewing.

Starting an exited container does not repair this, and neither does it pick up a newer image —
`docker start` reuses what the container was created with. Recreate it from the checkout you
mean to work in:

```bash
docker compose -f .devcontainer/docker-compose.yml up -d --build
```

That rebuilds the image and recreates the container against the mounts as they resolve here.
There is one `crypter-pipeline` on the machine, so this takes it over from whichever checkout
held it.

## Running a change

```bash
/crypter-change "<requirement>"
```

It plans, stops for your approval, then builds, examines, triages, remediates, opens the draft
pull request, and holds it against CI for at most three fix attempts. The approval is the only
stop, and the pull request stays a draft until you take it out of one.

`/crypter-review {pr-number}` is the second entry point. It fetches a pull request's head into
the container, runs the lenses against it with no plan to audit, then gives one verifier per
finding the same treatment `/crypter-triage-review` gives findings from anywhere else. Only the
findings that survive that are posted, as one review that comments. It never approves and never
requests changes.

What a lens raised and a verifier then ruled against stays in `.claude/runs`. It is a record of
the pipeline checking itself, and not something the pull request has to carry.

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

Both are required. Leaving one empty fails workspace creation with a message naming the
variable.

## Launching the container

The container is a Compose service in `.devcontainer/docker-compose.yml`. That is a separate
Compose project from the application stack at the repository root, so `docker compose up` and
`docker compose down` there never touch it, and the two share no network.

```bash
mkdir -p .claude/plans .claude/runs
docker compose -f .devcontainer/docker-compose.yml up -d --build
docker compose -f .devcontainer/docker-compose.yml exec pipeline bash
```

Create the two mount sources first. They are gitignored, so a fresh clone has neither, and
Docker creates a missing bind-mount source as root — which the orchestrators then cannot write
into.

The first `--build` takes a few minutes, mostly installing the `wasm-tools` workload. After
that Docker's layer cache makes it quick, and a change to `workspace.sh` rebuilds only the last
couple of layers. Use `--build` whenever `.devcontainer/` has changed; plain `up -d` otherwise.

Swap `up -d` for `down` to stop it. The named volumes outlive the container, so the next `up`
keeps your Claude Code credentials and your package caches.

## What is in the container

The image is built locally from `.devcontainer/Dockerfile` and tagged `crypter-devcontainer:local`.
It carries tooling and no source, so it only changes when the tooling does.

Built on `mcr.microsoft.com/dotnet/sdk:10.0`, running as an unprivileged user named `agent`
rather than as root:

- The .NET 10 SDK, the `wasm-tools` workload, and `dotnet-ef`
- Node 22 and pnpm 11.18.0, which `Crypter.Web`'s PreBuild target needs
- Claude Code

There is **no Docker in the container**, so `Crypter.Test` cannot run there — it needs
Testcontainers to start PostgreSQL. The agents build but never test locally; the test suite runs
in CI once the pull request exists, and failures come back to the implementer from there.

Three named volumes survive rebuilds, and none of them holds source:

| Volume | Holds |
|---|---|
| `crypter-pipeline-claude` | The agent's Claude Code state and credentials |
| `crypter-pipeline-nuget` | The NuGet package cache |
| `crypter-pipeline-pnpm` | The pnpm store |

The two caches exist because workspaces are ephemeral. Without them every run would restore
NuGet and pnpm from nothing, which is most of a build.

`/work` is the container's own filesystem rather than a volume, so live workspaces do not
survive a `down`. That is the intent: a run that was interrupted leaves nothing behind to
collide with the next one.

To start over from nothing, take the container down and remove the volumes:

```bash
docker compose -f .devcontainer/docker-compose.yml down
docker volume rm crypter-pipeline-claude crypter-pipeline-nuget crypter-pipeline-pnpm
```

## Authenticate Claude Code

The image ships Claude Code but no credentials. Run `claude` once inside the container and
follow the login prompt. The container has no browser, so the flow gives you a URL to open on
your host and a code to paste back.

Credentials live in `/home/agent/.claude`, which is the `crypter-pipeline-claude` volume, so
they survive container rebuilds. You only do this again after removing that volume.

Run the agents with `--permission-mode auto`. They work unattended, so a prompt they cannot
answer is a run that stalls. What bounds the blast radius is the container itself: a workspace
that is thrown away at the end of the run, a read-only view of your repository, and no GitHub
credential to push with.

## Changing the image

Needed when the tooling changes — a new tool the agents need, a runtime version bump. Source
changes never require it, because the image carries no source.

```bash
docker compose -f .devcontainer/docker-compose.yml up -d --build
```

That is the whole loop. The image is local to your machine — it is never published, and nobody
else consumes it — so a change to `workspace.sh` or the Dockerfile takes effect on your next
`up` and affects nothing but your own container.

`pr-build-devcontainer` builds the image on a pull request that touches `.devcontainer/`. It
pushes nothing; it is there to catch a Dockerfile that does not build.
