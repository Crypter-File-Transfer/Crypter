---
name: crypter-devcontainer-remediate
description: Apply a report to a branch the pipeline already built, whether triaged review findings or a CI failure. Invoked as /crypter-devcontainer-remediate {run-id} {branch} {report-path} by the crypter-change and crypter-triage-review skills.
---

# Crypter devcontainer remediate

Take a report of what is wrong with a branch this container already built, and fix it.

The branch exists in `/work/Crypter/.git`. Commit locally; the result is fetched out and pushed
once you return.

The report is triaged review findings or a CI failure. Both are the same job: a description of
what is wrong, an existing branch, and commits that address it.

## Setup

You are given a run id, a branch name, and a report path:
`/crypter-devcontainer-remediate {run-id} {branch} {report-path}`.

Read the report first. It lives under `/runs/{run-id}/`, the mount the host shares with you.
**If it is absent, stop and say so.**

Read `/plans/{run-id}/plan.md` too where one exists. The fix stays inside what the plan set out
to do; a repair that reaches into the plan's non-goals belongs in your report rather than in a
commit.

## 1. Worktree on the existing branch

```bash
git -C /work/Crypter fetch upstream
git -C /work/Crypter worktree add /work/Crypter/.claude/worktrees/{run-id} {branch}
```

No `-b` — the branch is already there, carrying the commits the host has pushed. **If this
fails, stop and say so.**

## 2. Fix

Invoke `implementer` with the report path and the worktree path. Each fix is its own commit on
the branch.

Read its report. If it says the failure could not be addressed, say so plainly in your own
report rather than reporting success.

## 3. Hand off

```bash
git -C /work/Crypter worktree remove /work/Crypter/.claude/worktrees/{run-id}
```

Remove it on every exit path. The branch keeps the new commits.

Then report back to the host session: what the report described, what changed, and which commits
now sit on the branch. The host fetches those commits and pushes them.
