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

**Run the orchestrators from the root of a checkout**, main or worktree. `.claude/plans` and
`.claude/runs` are mounts relative to `.devcontainer/`, so they resolve against whichever checkout
you launch from, and the container is named after that checkout — so the one you reach is always
the one holding the artifacts you are reading.

**The container holds no GitHub credential.** It clones anonymously over https and can only read
a public repository. Every authenticated GitHub operation happens in your session with your own
access, and `/crypter-change` pushes and re-pushes without stopping to ask.

The branch is pushed to the org repository and the pull request opens against it, base `stable`,
the same route a branch of your own takes. `/crypter-change` leaves you a draft pull request to
read.

The container does hold your Claude Code credential, in the `crypter-pipeline-claude` volume or as
`CLAUDE_CODE_OAUTH_TOKEN` in its environment, and its network egress is open. Treat it as a trust
boundary rather than a sandbox.

## Workspaces

The agents build and review in a **workspace**: a clone of the repository at `/work/{run-id}`,
made when a run starts and deleted when it ends. Nothing that holds a copy of the code outlives
the run that made it, so there is no second checkout drifting away from yours.

Workspaces are cloned from `CRYPTER_REPO_URL` — the org repository on GitHub, not your checkout.
Nothing on the host is mounted for them to read. **Your checkout is therefore irrelevant to what
a run builds or reviews**: it sees the branch as the repository holds it, whatever yours is on,
however stale it is, and whatever is uncommitted in it. A pull request head is fetched straight
from `refs/pull/{number}/head`, so nothing has to be staged on your side first.

Because each workspace is a full clone taken when it is created, and nothing re-fetches
afterwards, a run is pinned to the commit it started from. Merge to `stable` while a run is going
and it keeps building against what it cloned; the next run gets the new commit. Two runs at
different bases are no trouble.

The orchestrator owns the lifecycle. It creates the workspace in its setup and removes it when
the run ends; the container skills use it and never create or destroy one.

```bash
.devcontainer/pipeline.sh exec -- crypter-workspace create {run-id} [--base {branch}] [refspec]
.devcontainer/pipeline.sh exec -- crypter-workspace remove {run-id}
```

`--base` is the branch the run is built or reviewed against, `stable` when it is not given. A
pull request states its own base, and a release states `main`, so the review skills pass what
they read rather than assuming.

This document covers the setup you need before the container will start.

## The mounts

Everything crossing the container boundary goes through one of these:

| Host | Container | Direction | Holds |
|---|---|---|---|
| `.claude/plans` | `/plans` | Read-only | `{run-id}/plan.md` |
| `.claude/runs` | `/runs` | Writable | `{run-id}/conformance.md`, `{run-id}/findings/{lens}.md`, `{run-id}/review.md`, `{run-id}/verification/{id}.md`, `{run-id}/triage.md`, `{run-id}/ci-{n}.md` |

Both are gitignored and live on your disk. Only `/runs` is writable; `/plans` the container can
read and nothing more.

Source is not among them. Nothing of your repository is mounted, so a run cannot read your
working tree, your local branches, or a ref you have not pushed — it clones from GitHub instead.

The plan goes in and cannot be rewritten by the agents. Findings come back out as files you can
open, grep and keep, rather than as text in a transcript, and each is written by the agent that
found it. `triage.md` is what `/crypter-change` decided to act on, and reading it is how you
check that judgement.

The container's `agent` user is uid 1001, because the base image already has a user on 1000. A
bind mount keeps host ownership, so the orchestrators create every directory under `.claude/runs`
themselves and give it mode 777. Directories made on the host stay deletable from the host; a
directory the container creates is one you need `docker exec` to remove.

A branch the agents built travels back out the same way it would from any remote, over a git
transport that runs `docker exec` instead of opening a socket:

```bash
git -c protocol.ext.allow=user fetch \
  "ext::docker exec -i $(.devcontainer/pipeline.sh name) git upload-pack /work/{run-id}" {branch}:{branch}
```

`protocol.ext.allow` is passed per command, so it stays out of your git config. Pushing is then
yours, with your credentials — which is what keeps a write token out of a container running
unattended agents.

## One container per checkout

`/plans` and `/runs` are relative paths in the Compose file, so they resolve against the checkout
you launch from and a container is stuck with whatever they resolved to when it was created. Each
checkout therefore gets its own container, named after it:

```bash
.devcontainer/pipeline.sh name        # crypter-pipeline-crypter-4f3a9c21
```

The name is derived from the checkout's real path — a readable slug, and a hash to separate two
clones that share a basename. Nothing to configure, and nothing that can drift out of step with
where the checkout actually is. Because a checkout resolves only to its own name, a run can never
reach another checkout's mounts, and recreating one container leaves the others alone.

```bash
.devcontainer/pipeline.sh list        # every instance, and the checkout it belongs to
```

This is not what makes runs parallel. Several runs share one container quite happily — they are
separated by their workspaces at `/work/{run-id}` and their artifacts at `/runs/{run-id}`, and
concurrent `docker exec` calls do not queue. Per-checkout naming is about which host directories
a container is wired to, nothing more.

Starting an exited container does not pick up a newer image — `docker start` reuses what the
container was created with. Recreate it instead:

```bash
.devcontainer/pipeline.sh up
```

That rebuilds the image and recreates this checkout's container. `/work` is the container's own
filesystem rather than a volume, so this destroys any workspace a run in this checkout is still
using.

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

| Variable | Value | |
|---|---|---|
| `CRYPTER_GIT_NAME` | Author name on the agents' commits. | Required |
| `CRYPTER_GIT_EMAIL` | Author email on the agents' commits. | Required |
| `CLAUDE_CODE_OAUTH_TOKEN` | Claude Code's credential. | Optional; an alternative to `pipeline.sh login` |
| `CRYPTER_REPO_URL` | The repository workspaces are cloned from. | Defaults to the org repository; set it for a fork |

The two git variables fail workspace creation with a message naming the variable when left empty.

The container's name needs no configuration. It is derived from where the checkout is.

## Launching the container

The container is a Compose service in `.devcontainer/docker-compose.yml`, driven through
`pipeline.sh` — which supplies the per-checkout name Compose needs. That is a separate Compose
project from the application stack at the repository root, so `docker compose up` and
`docker compose down` there never touch it, and the two share no network.

```bash
mkdir -p .claude/plans .claude/runs
.devcontainer/pipeline.sh up
.devcontainer/pipeline.sh exec -- bash        # add -it for an interactive shell
```

Create the two mount sources first. They are gitignored, so a fresh clone has neither, and
Docker creates a missing bind-mount source as root — which the orchestrators then cannot write
into.

Running `docker compose` against this file directly fails, on purpose: the container name is a
required variable, and a container created without it would say nothing about which checkout's
mounts it holds.

The first `up` takes a few minutes, mostly installing the `wasm-tools` workload. After that
Docker's layer cache makes it quick, and a change to `workspace.sh` rebuilds only the last couple
of layers.

`pipeline.sh down` stops it. The named volumes outlive the container, so the next `up` keeps your
Claude Code credentials and your package caches.

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

It does need **outbound network**, both for the Anthropic API and now for cloning workspaces.
The pipeline does not work offline.

Three named volumes survive rebuilds, and none of them holds source:

| Volume | Holds |
|---|---|
| `crypter-pipeline-claude` | The agent's Claude Code state and credentials |
| `crypter-pipeline-nuget` | The NuGet package cache |
| `crypter-pipeline-pnpm` | The pnpm store |

**Every instance on the machine shares all three**, which is why Claude Code is authenticated
once rather than once per checkout, and why a second checkout's first build is not a cold
restore. They are declared external so that several Compose projects can mount them; `pipeline.sh
up` creates them.

The two caches exist because workspaces are ephemeral. Without them every run would restore
NuGet and pnpm from nothing, which is most of a build.

`/work` is the container's own filesystem rather than a volume, so live workspaces do not
survive a `down`. That is the intent: a run that was interrupted leaves nothing behind to
collide with the next one.

To start over from nothing, take the container down and remove the volumes:

```bash
.devcontainer/pipeline.sh down
docker volume rm crypter-pipeline-claude crypter-pipeline-nuget crypter-pipeline-pnpm
```

The `down` is per-checkout, but removing the volumes is not — it takes the credentials and caches
away from **every** instance. Use `pipeline.sh list` to see what else is on the machine first,
including containers left behind by checkouts that no longer exist.

## Authenticate Claude Code

The image ships Claude Code but no credential. Log in once:

```bash
.devcontainer/pipeline.sh login
```

Type `/login` and follow the prompt. The container has no browser, so the flow gives you a URL to
open on your host and a code to paste back. Credentials live in `/home/agent/.claude`, which is
the `crypter-pipeline-claude` volume, so they survive rebuilds and are shared by every checkout
on the machine. Run this again when the login lapses.

A machine that would rather configure the credential than open a browser can set
`CLAUDE_CODE_OAUTH_TOKEN` in `.devcontainer/.env` instead, generated on the host with `claude
setup-token`. Compose reads it into the container's environment and Claude Code uses it in place
of the stored login. Replacing an expired one means editing `.env` and running `up` again, since
the environment is fixed when the container is created — a stored login renews without that.

`up` warns when neither is set. Either one on its own is enough.

Run the agents with `--permission-mode auto`. They work unattended, so a prompt they cannot
answer is a run that stalls. What bounds the blast radius is the container itself: a workspace
that is thrown away at the end of the run, no access to your repository at all, and no GitHub
credential to push with — its only reach into the repository is an anonymous read of what is
already public.

## Changing the image

Needed when the tooling changes — a new tool the agents need, a runtime version bump. Source
changes never require it, because the image carries no source.

```bash
.devcontainer/pipeline.sh up
```

That is the whole loop. The image is local to your machine — it is never published, and nobody
else consumes it — so a change to `workspace.sh` or the Dockerfile takes effect on your next
`up`.

The image tag is shared, so the rebuild is machine-wide; other instances pick the new image up
when they are next recreated, not before.

`pr-build-devcontainer` builds the image on a pull request that touches `.devcontainer/`. It
pushes nothing; it is there to catch a Dockerfile that does not build.
