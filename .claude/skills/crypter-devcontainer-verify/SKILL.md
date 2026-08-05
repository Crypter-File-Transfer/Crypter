---
name: crypter-devcontainer-verify
description: Rule on each finding in a report against the code, one verifier per finding. Invoked as /crypter-devcontainer-verify {run-id} {ref} {findings-path} by the crypter-triage-review skill.
---

# Crypter devcontainer verify

Take a list of findings somebody left on a diff and decide which of them are true.

The ref already exists in the run's workspace at `/work/{run-id}`. You write verdicts and
nothing else — no fixes,
and no findings of your own.

## Setup

You are given a run id, a ref, and a findings path:
`/crypter-devcontainer-verify {run-id} {ref} {findings-path}`.

The findings file lives under `/runs/{run-id}/`. Each finding in it carries an id. **If the file
is absent, stop and say so.**

`/runs/{run-id}/verification/` already exists; the caller creates it. **If it is missing, stop
and say so** rather than creating it.

## 1. Put the workspace on the ref

The workspace at `/work/{run-id}` already exists; the host created it. **If it is missing, stop
and say so** rather than creating one.

```bash
git -C /work/{run-id} checkout --detach {ref}
```

`--detach` because you only read. **If this fails, stop and say so.**

Every agent gets the workspace path and works by absolute path inside it. Never `cd`.

## 2. Verify

Invoke `finding-verifier` once per finding, in parallel. Each gets one finding, the workspace
path, and `/runs/{run-id}/verification/{finding-id}.md`.

One finding per agent, and each sees only its own. A verifier that reads the whole report starts
weighing findings against each other instead of against the code.

Never give a finding to the agent that raised it.

## 3. Report

For each finding: its id, the verdict, and one line of evidence. Then the counts — how many held,
how many did not, how many are unsettled. Name the files you wrote.

Leave the workspace as it is. The host removes it when the run ends.
