---
name: pipeline-fix
description: Apply a fix to a branch the pipeline already built, from a report the host wrote. Invoked as /pipeline-fix {run-id} {branch} {report-path} by the crypter-publish skill on the host.
---

# Pipeline fix

Take a report of something wrong with a branch this container already built, and fix it.

**This runs inside the devcontainer**, on a branch that exists in `/work/Crypter/.git` from an
earlier `/pipeline` run. The host wrote the report, and the host publishes the result.

## Setup

You are given a run id, a branch name, and a report path: `/pipeline-fix {run-id} {branch}
{report-path}`.

Read the report first. It lives under `/plans/{run-id}/`, the read-only mount the host owns.
**If it is absent, stop and say so.**

Read `/plans/{run-id}/plan.md` too. The fix stays inside what the plan set out to do.

## 1. Worktree on the existing branch

```bash
git -C /work/Crypter fetch upstream
git -C /work/Crypter worktree add /work/Crypter/.claude/worktrees/{run-id} {branch}
```

No `-b` — the branch is already there, with the commits the host has pushed. **If this fails,
stop and say so.**

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

Then report back to the host session: what the failure was, what changed, and which commits
now sit on the branch. The host fetches those commits and pushes them.
