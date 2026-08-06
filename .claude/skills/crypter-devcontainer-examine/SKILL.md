---
name: crypter-devcontainer-examine
description: Review a diff in the pipeline container and write findings to the host. Invoked as /crypter-devcontainer-examine {run-id} {ref} {base-ref} [plan-path] by the crypter-change and crypter-review skills.
---

# Crypter devcontainer examine

Review a diff and leave hard artifacts behind. You do not write code and you do not decide what
gets acted on; your caller triages what you find.

The ref already exists in the run's workspace at `/work/{run-id}`.

## Setup

You are given a run id, a ref, a base ref, and optionally a plan path:
`/crypter-devcontainer-examine {run-id} {ref} {base-ref} [plan-path]`.

The base ref is the branch the change is proposed against, named as the workspace knows it —
`origin/stable` for work built here, `origin/main` for a pull request that targets `main`. Every
agent below diffs against it. **Pass it on as given; never substitute a default.** A base that
does not match the pull request produces a diff nobody asked about, and the emptiest version of
that failure — a base identical to the ref — reads as four lenses finding nothing wrong.

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

## 1. Put the workspace on the ref

The workspace at `/work/{run-id}` already exists; the host created it. **If it is missing, stop
and say so** rather than creating one.

```bash
git -C /work/{run-id} checkout --detach {ref}
```

`--detach` because you only read. Leaving the branch unclaimed keeps a later stage free to check
it out and commit to it. **If this fails, stop and say so.**

Every agent gets the workspace path and works by absolute path inside it. Never `cd`.

## 2. Plan adherence

Given a plan path, invoke `conformance-auditor` with it, the workspace, the base ref, and
`/runs/{run-id}/conformance.md`. It reports where the diff and the plan diverge.

## 3. Code review

Invoke `reviewer` once per lens, in parallel — they do not interact. Each gets the workspace, the
base ref, and `/runs/{run-id}/findings/{lens}.md`.

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

Leave the workspace as it is. The host removes it when the run ends.
