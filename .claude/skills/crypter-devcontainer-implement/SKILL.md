---
name: crypter-devcontainer-implement
description: Build an approved plan into commits on a new branch, inside the pipeline container. Invoked as /crypter-devcontainer-implement {run-id} {branch} by the crypter-change skill.
---

# Crypter devcontainer implement

Turn an approved plan into commits on a branch.

The workspace is an anonymous clone of the org repository with a single remote, `upstream`,
which has no push url. Commit locally and stop there; the branch is fetched out and pushed once
you return.

The plan is the specification. The user approved it before this ran, and this runs unattended.

## Setup

You are given a run id and a branch name: `/crypter-devcontainer-implement {run-id} {branch}`.

Read `/plans/{run-id}/plan.md` first. It is a read-only mount of the host's `.claude/plans`.
**If it is absent, stop and say so** — the host session owns that file.

## 1. Sync and branch

Build on current code:

```bash
git -C /work/Crypter fetch upstream
git -C /work/Crypter worktree add /work/Crypter/.claude/worktrees/{run-id} -b {branch} upstream/stable
```

**If either fails, stop and say so.** A quietly skipped sync leaves the diff and the eventual
pull request on the wrong base, and nothing downstream will notice.

Work by absolute path inside the worktree. Never `cd`.

## 2. Implement

Invoke `implementer` with `/plans/{run-id}/plan.md` and the worktree path. Give it nothing about
how the plan was reached — the plan is the specification.

Read its report. If it says a step could not be done, that is not a failure to paper over:
say so plainly in your own report.

## 3. Hand off

```bash
git -C /work/Crypter worktree remove /work/Crypter/.claude/worktrees/{run-id}
```

Remove it on every exit path. The branch ref lives in `/work/Crypter/.git` and survives, which
is what the host fetches.

Then report back to the host session:

- The branch name and the commits on it.
- A title and description for the pull request. Title reads like a commit subject: imperative,
  capitalized, no trailing period. Description is a few sentences of plain English saying what
  changed and why, written for the org repository's reviewers. **Do not argue the case** — no
  justifying the approach, no pre-empting objections, no listing rejected alternatives. Call out
  what a reviewer would otherwise have to discover: migrations, breaking API changes,
  deliberately held-back dependencies. That is information, not argument.
- Anything the implementer could not do.
