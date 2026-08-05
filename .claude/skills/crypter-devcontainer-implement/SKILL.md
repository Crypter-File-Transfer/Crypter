---
name: crypter-devcontainer-implement
description: Build an approved plan into commits on a new branch, inside the pipeline container. Invoked as /crypter-devcontainer-implement {run-id} {branch} by the crypter-change skill.
---

# Crypter devcontainer implement

Turn an approved plan into commits on a branch.

The workspace at `/work/{run-id}` is a clone of the host repository, taken from a read-only
mount. It has no push url and no credential. Commit locally and stop there; the branch is
fetched out and pushed once you return.

The plan is the specification. The user approved it before this ran, and this runs unattended.

## Setup

You are given a run id and a branch name: `/crypter-devcontainer-implement {run-id} {branch}`.

Read `/plans/{run-id}/plan.md` first. It is a read-only mount of the host's `.claude/plans`.
**If it is absent, stop and say so** — the host session owns that file.

`/work/{run-id}` already exists; the host created it. **If it is missing, stop and say so**
rather than creating one — the host owns the workspace for the whole run and removes it at the
end.

## 1. Branch

The workspace is checked out at `upstream/stable`, so build from there:

```bash
git -C /work/{run-id} checkout -b {branch} refs/remotes/upstream/stable
```

**If this fails, stop and say so.** A branch cut from the wrong base leaves the diff and the
eventual pull request on the wrong base, and nothing downstream will notice.

Work by absolute path inside the workspace. Never `cd`.

## 2. Implement

Invoke `implementer` with `/plans/{run-id}/plan.md` and the workspace path. Give it nothing about
how the plan was reached — the plan is the specification.

Read its report. If it says a step could not be done, that is not a failure to paper over:
say so plainly in your own report.

## 3. Hand off

Leave the workspace as it is, with `{branch}` checked out and its commits on it. The host fetches
the branch out of it and removes it when the run ends.

Report back to the host session:

- The branch name and the commits on it.
- A title and description for the pull request. Title reads like a commit subject: imperative,
  capitalized, no trailing period. Description is a few sentences of plain English saying what
  changed and why, written for the org repository's reviewers. **Do not argue the case** — no
  justifying the approach, no pre-empting objections, no listing rejected alternatives. Call out
  what a reviewer would otherwise have to discover: migrations, breaking API changes,
  deliberately held-back dependencies. That is information, not argument.
- Anything the implementer could not do.
