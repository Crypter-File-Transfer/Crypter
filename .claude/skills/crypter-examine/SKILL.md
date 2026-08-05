---
name: crypter-examine
description: Review a diff in the pipeline container and write findings to the host. Invoked as /crypter-examine {run-id} {ref} [plan-path] by the crypter-change and crypter-review skills on the host.
---

# Crypter examine

Review a diff and leave hard artifacts behind. You do not write code and you do not decide what
gets acted on; the host session triages what you find.

**This runs inside the devcontainer**, against a ref that already exists in `/work/Crypter/.git`.

## Setup

You are given a run id, a ref, and optionally a plan path:
`/crypter-examine {run-id} {ref} [plan-path]`.

Two review phases run here, and the plan path decides whether the first one applies:

| Phase | Runs when |
|---|---|
| Plan adherence | A plan path is given |
| Code review | Always |

A change built from a plan gets both. A pull request someone else raised gets the second alone,
since there is no plan to hold it against.

Findings go to `/runs/{run-id}/`, a writable mount of the host's `.claude/runs`. Each agent
writes its own findings; nothing here rewrites or summarises them into a second copy. They are
the deliverable — the host reads these files to triage, the user reads them to check that
judgement, and a later pass can read them to verify the claims they make.

`/runs/{run-id}/` and `/runs/{run-id}/findings/` already exist; the caller creates them. **If
either is missing, stop and say so** rather than creating it — a directory made on this side is
one the host cannot clean up.

## 1. Worktree on the ref

```bash
git -C /work/Crypter worktree add --detach /work/Crypter/.claude/worktrees/{run-id} {ref}
```

`--detach` because you only read. A worktree that claims the branch collides with anything else
holding it, and reviewing never needs it claimed. **If this fails, stop and say so.**

Every agent gets this worktree path and works by absolute path inside it. Never `cd`.

## 2. Plan adherence

Given a plan path, invoke `conformance-auditor` with it, the worktree, and
`/runs/{run-id}/conformance.md`. It reports where the diff and the plan diverge.

## 3. Code review

Invoke `reviewer` once per lens, in parallel — they do not interact. Each gets the worktree and
`/runs/{run-id}/findings/{lens}.md`.

| Lens | Brief |
|---|---|
| correctness | Bugs, boundary conditions, error paths, and what happens when inputs are hostile or absent. |
| maintainability | Readability, scope creep, and the conventions in `CLAUDE.md` and the Coding Standard. |
| testability | What the tests pin down, what they leave unverified, and whether the change can be tested at all. |
| security | Crypto boundaries, input validation, authentication and authorisation paths, key handling, transfer integrity. |

Adding a lens means adding a row here. The `reviewer` definition stays as it is; the lens comes
from the prompt.

Run the phases in parallel with each other too. The auditor and the reviewers read the same diff
and never interact.

## 4. Report

Summarise for the host session: how many findings each lens raised, where the auditor found
drift, and which findings you would look at first. Name the files you wrote.

Leave the judgement to the host. Reporting a finding is not accepting it.

```bash
git -C /work/Crypter worktree remove /work/Crypter/.claude/worktrees/{run-id}
```

Remove it on every exit path.
