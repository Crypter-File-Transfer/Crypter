---
name: crypter-change
description: Take a requirement to an open, CI-green draft pull request, orchestrating the plan, implement, examine and pull request skills. Use when asked to make a change to Crypter, or invoked as /crypter-change "<requirement>".
---

# Crypter change

Carry a requirement from a sentence to a draft pull request whose checks pass.

You own the whole run. Building and reviewing happen in the container; you hold the plan, the
findings and every CI attempt, which is why the judgement calls are yours.

Run from the root of the main checkout. The container's mounts resolve against it, so a run
started from a worktree writes its plan where the container cannot read it.

There is one gate: the user approves the plan. Everything after it runs to a green draft pull
request, or to a written account of why CI would not take it.

## Setup

Pick a short run id from the requirement — `transfer-limits`, `fix-expiry-tz` — and a branch
named as the repo does: `feature/{something}`, `fix/{something}`, `chore/{something}`. Both stay
fixed for the run.

```bash
mkdir -p .claude/plans/{run-id} .claude/runs/{run-id}/findings
chmod 777 .claude/runs/{run-id} .claude/runs/{run-id}/findings
```

`.claude/plans/{run-id}` is what the container reads; `.claude/runs/{run-id}` is where the
reviewing agents write their findings and where you write what you decide. Both are gitignored,
and both are yours to read at any point.

**Make every directory under `/runs` here, and make it `777`.** The container's `agent` is uid
1001 and your files are uid 1000, and a bind mount keeps host ownership, so the agents can only
write into a directory that grants it. Creating them on this side also keeps you able to delete
what they wrote — a directory the container creates is one you cannot remove.

The container needs both mounts, the run directory has to be writable from inside it, and the
image has to carry the current tooling. Confirm before starting:

```bash
docker exec crypter-pipeline test -d /plans/{run-id} && \
  docker exec crypter-pipeline test -w /runs/{run-id}/findings && \
  docker exec crypter-pipeline test -x /usr/local/bin/crypter-workspace
```

The mount checks also settle which checkout the container belongs to: one created against a
different one reaches neither directory. The last check is separate because an older image
passes the first two and then fails at workspace creation with nothing but a missing executable
to go on. All three are `test` because `docker exec` runs a binary and not a shell, so a builtin
like `command -v` exits 127 whether or not the thing it was looking for is there.

**Do not `docker start` an exited container to fix any of this.** Mounts and image are fixed
when a container is created, so starting one built from another checkout, or from an older
image, brings back the same wrong container. Bring it up from here instead:

```bash
docker compose -f .devcontainer/docker-compose.yml up -d --build
```

That rebuilds the image and recreates the container against this checkout's mounts. It replaces
any container of the same name, so **ask the user before running it** — theirs may belong to
another checkout and hold work you cannot see.

Then make the workspace the container builds in. It is a clone of your repository, taken from
the read-only `/host-git` mount, and it lasts exactly as long as this run:

```bash
git fetch origin
docker exec crypter-pipeline crypter-workspace create {run-id}
```

Fetch first — the workspace takes `upstream/stable` from your `origin/stable`, so a stale
remote-tracking ref puts the whole run on an old base. **If either fails, stop and say so.**

The workspace holds only committed history. Uncommitted work in your checkout is not visible to
the container and never reaches the branch.

## 1. Plan

Invoke `crypter-step-plan` with the requirement verbatim and the output path
`.claude/plans/{run-id}/plan.md`.

It settles the plan with the user itself. **Do not continue until they have approved it.**

## 2. Build

```bash
docker exec -w /work/{run-id} crypter-pipeline \
  claude --permission-mode auto -p "/crypter-devcontainer-implement {run-id} {branch}"
```

Keep the title and description it reports; `crypter-step-open-pull-request` needs them.

## 3. Examine

```bash
docker exec -w /work/{run-id} crypter-pipeline \
  claude --permission-mode auto -p "/crypter-devcontainer-examine {run-id} {branch} /plans/{run-id}/plan.md"
```

It writes `.claude/runs/{run-id}/conformance.md` and `.claude/runs/{run-id}/findings/{lens}.md`.
Read the files, not the summary.

## 4. Triage

You decide what to act on. Read every finding against the code before accepting it — a reviewer
that has already been wrong once will happily be wrong again, and acting on a bad finding means
changing working code.

Accept anything with a concrete failure behind it. Reject preferences, restatements of the plan
the user already chose against, and findings about code the diff did not touch. An unplanned
extra that contradicts the plan's non-goals is not a preference — accept it.

Write what you accepted and what you rejected, with a reason for each rejection, to
`.claude/runs/{run-id}/triage.md`. The user reads this to check your judgement, so write it for
them.

## 5. Remediate

Where anything was accepted:

```bash
docker exec -w /work/{run-id} crypter-pipeline \
  claude --permission-mode auto -p "/crypter-devcontainer-remediate {run-id} {branch} /runs/{run-id}/triage.md"
```

## 6. Open the pull request

Invoke `crypter-step-open-pull-request` with the run id and the branch. It fetches the commits
out of the container, pushes them, and opens or updates the draft pull request.

## 7. Hold it against CI

Invoke `ci-watcher` with the repository path, the branch, the pull request number, the attempt
number, and `.claude/runs/{run-id}/ci-{n}.md`. It runs one attempt and reports.

The loop is yours:

1. Green → go to stage 8.
2. A failure → run `crypter-devcontainer-remediate` with `/runs/{run-id}/ci-{n}.md`, invoke
   `crypter-step-open-pull-request` again, then `ci-watcher` with the next attempt number.
3. **Three attempts is the ceiling.** Comment the state of play on the pull request and hand back
   to the user.

Stop earlier and ask the user whenever another attempt looks pointless — the same check failing
the same way twice, a failure the plan did not anticipate, or anything that reads as a wrong plan
rather than wrong code. Three attempts is a limit, not a quota to spend.

Stop immediately, without spending an attempt, where `ci-watcher` reports that no run appeared
for the commit. Nothing to fix has been established yet, and a push that starts no checks is a
setup problem rather than a code one.

## 8. Tear down and report

The branch is on the fork and the artifacts are on your disk, so the workspace has nothing left
to hold:

```bash
docker exec crypter-pipeline crypter-workspace remove {run-id}
```

Remove it on every exit path, including the ones where you stopped early. Nothing under `/runs`
or `.claude/plans` is touched by this — those are the record of the run and they stay.

Then report:

- The pull request URL and whether its checks are green. It is a draft; taking it out of draft
  is the user's.
- What each fix attempt changed, where any ran.
- Anything the implementer could not do, and any drift the auditor flagged.
- What you rejected in triage that the user might disagree with, and where `triage.md` is.
