---
name: pipeline
description: Take an approved plan to a reviewed branch in the pipeline container, using a chain of subagents. Invoked as /pipeline {run-id} {branch} by the crypter-plan-author skill on the host.
---

# Pipeline

Turn an approved plan into a reviewed branch, in stages, each run by a subagent with its own
context. A later stage that starts fresh actually re-examines the work; one that inherits the
reasoning behind it rubber-stamps it.

**This runs inside the devcontainer.** The workspace is an anonymous clone of the org
repository with a single remote, `upstream`, which has no push url. The container holds no
credential and reads public code. The host session pushes the branch and opens the pull
request once you return.

The plan is the specification. An interactive session on the host wrote it under the
`crypter-plan-author` skill and the user approved it there. This runs unattended, to a branch
the host can publish.

## Setup

You are given a run id and a branch name: `/pipeline {run-id} {branch}`.

Read `/plans/{run-id}/plan.md` first. It is a read-only mount of the host's `.claude/plans`.
**If it is absent, stop and say so** — the host session owns that file.

Run state goes in the workspace, which is writable:

```bash
mkdir -p /work/Crypter/.claude/pipeline/{run-id}
```

`conformance.md` and `findings/` go there. It is gitignored.

## 1. Sync and branch

Build on current code:

```bash
git -C /work/Crypter fetch upstream
git -C /work/Crypter worktree add /work/Crypter/.claude/worktrees/{run-id} -b {branch} upstream/stable
```

**If either fails, stop and say so.** A quietly skipped sync leaves the diff and the eventual
pull request on the wrong base, and nothing downstream will notice.

Every later stage gets this worktree path and works by absolute path inside it. Never `cd`.

## 2. Implement

Invoke `implementer` with `/plans/{run-id}/plan.md` and the worktree path. The plan is the
specification.

Read its report. If it says a step could not be done, that is not a failure to paper over:
surface it to the user with the rest of the results at the end, and let the auditor record it.

## 3. Examine

Run these in parallel — they do not interact:

- `conformance-auditor` with `/plans/{run-id}/plan.md`, the worktree, and
  `/work/Crypter/.claude/pipeline/{run-id}/conformance.md`.
- `reviewer`, once per lens, with the worktree and
  `/work/Crypter/.claude/pipeline/{run-id}/findings/{lens}.md`.

The lens list is currently one entry:

| Lens | Brief |
|---|---|
| general | Correctness and edge cases first, then scope creep, then the conventions in `CLAUDE.md`. |

Adding lenses later — security, simplicity, test coverage — means adding rows here. The
`reviewer` definition does not change; the lens comes from the prompt.

## 4. Triage

You decide what to act on. Read every finding against the code before accepting it — a reviewer
that has already been wrong once will happily be wrong again, and acting on a bad finding means
changing working code.

Accept anything with a concrete failure behind it. Reject preferences, restatements of the plan
you already chose against, and findings about code the diff did not touch. An unplanned extra
that contradicts the plan's non-goals is not a preference — accept it.

Write what you accepted and what you rejected, with a reason for each rejection, into
`/work/Crypter/.claude/pipeline/{run-id}/findings/triage.md`. The user reads this to check your
judgement.

## 5. Remediate

If anything was accepted, invoke `implementer` with the accepted findings and the worktree
path. Each fix is its own commit on the existing branch.

## 6. Hand off

```bash
git -C /work/Crypter worktree remove /work/Crypter/.claude/worktrees/{run-id}
```

Remove it on every exit path, including when the pipeline stopped early. The branch ref lives
in `/work/Crypter/.git` and survives, which is what the host fetches.

Then report back to the host session, in a few sentences:

- The branch name, and a title and description for the pull request. Title reads like a commit
  subject: imperative, capitalized, no trailing period. Description is a few sentences of plain
  English saying what changed and why, written for the org repository's reviewers. **Do not
  argue the case** — no justifying the approach, no pre-empting objections, no listing rejected
  alternatives. Call out what a reviewer would otherwise have to discover: migrations, breaking
  API changes, deliberately held-back dependencies. That is information, not argument.
- Anything the implementer could not do, and any deviation the auditor flagged as drift.
- What you rejected in triage that the user might disagree with.

The host session pushes the branch and opens the pull request from there.
