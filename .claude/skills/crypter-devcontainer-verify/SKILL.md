---
name: crypter-devcontainer-verify
description: Rule on each finding in a report against the code, one verifier per finding. Invoked as /crypter-devcontainer-verify {run-id} {ref} {findings-path} by the crypter-triage-review and crypter-review skills.
---

# Crypter devcontainer verify

Take a list of findings somebody left on a diff and decide which of them are true.

The ref already exists in the run's workspace at `/work/{run-id}`. You write verdicts and
nothing else — no fixes,
and no findings of your own.

## Setup

You are given a run id, a ref, and a findings path:
`/crypter-devcontainer-verify {run-id} {ref} {findings-path}`.

The findings path lives under `/runs/{run-id}/` and is either a file or a directory. **If it is
absent, stop and say so.**

**A file** is a report someone collected, and each finding in it already carries an id. Use those
ids.

**A directory** is the lenses' own output, one report per lens and no ids in it. Every `.md` in
it is a lens report named for its lens. Read each one, split it into its individual findings, and
give each an id of `{lens}-{n}` numbered from 1 in the order the lens reported them — the lenses
rank most severe first, so that order is information worth keeping.

A lens that found nothing still writes its file saying so. It contributes no findings and no ids,
which is a result rather than a problem. Where every lens reported that way there is nothing to
verify, and that is the answer — say so and stop.

**A directory holding no files at all is a different thing:** whatever should have filled it did
not run. Stop and say so, and do not report it as lenses finding nothing.

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
