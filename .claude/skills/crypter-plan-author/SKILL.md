---
name: crypter-plan-author
description: Draft an implementation plan for Crypter interactively, then hand it to the pipeline container to build. Use when asked to plan a change, or invoked as /crypter-plan-author "<requirement>".
---

# Crypter plan author

You turn a requirement into a plan the pipeline's agents implement. They see the plan and
nothing else. Write for that reader.

This runs on the host, with the web, the user's tooling, and the user available to you. Settle
anything that needs them here, and write the answer into the plan.

## 1. Sync

```bash
git fetch upstream
git fetch origin
```

Read the code at `upstream/stable`, the commit the container branches from.

## 2. Pick a run id and a branch

A short run id from the requirement — `transfer-limits`, `fix-expiry-tz`. Name the branch as
the repo does: `feature/{something}`, `fix/{something}`, `chore/{something}`.

Both are passed to the pipeline in stage 6, so decide them now and keep them stable.

## 3. Understand before deciding

Read `CLAUDE.md` and `Documentation/Development/Coding Standard.md` first. Then read the code
the requirement touches, and the code around it — the existing patterns are the ones the
implementation matches.

Prefer reusing what exists. If a monad, primitive, service, or extension already does most of
the job, name it in the plan with its path.

## 4. Write the plan

Write it to `.claude/plans/{run-id}/plan.md`, the host side of the container's read-only
`/plans` mount. Create the directory as needed.

- **Goal** — one paragraph. What changes for a user of Crypter, and why.
- **Non-goals** — what this change deliberately leaves alone. Be specific; this keeps the
  implementer in scope, and the conformance auditor checks against it.
- **Approach** — the design, in prose. Name the types and methods to add or change. Explain
  anything non-obvious, especially where a constraint forced the shape.
- **Steps** — numbered and ordered, each naming the files it touches. A step should be small
  enough that its result is obvious.
- **Tests** — what to add to `Crypter.Test` or `Crypter.Test.Web` and what each case pins
  down. CI is where the suite runs, so tests are what verify behaviour.
- **Risks** — what could break, and what a reviewer should look at hardest.

### Crypter's idioms are part of the plan

Express the plan in the conventions the code already uses, so the implementer inherits them:

- `Maybe<T>` and `Either<TLeft, TRight>` from `Crypter.Common/Monads` for expected failures.
- Validated types from `Crypter.Common/Primitives` rather than raw strings.
- `Async` suffix on async methods, and async all the way for database, file, and network IO.
- Constructors over object initializers. Enums over magic strings.
- Any change to an entity under `Crypter.DataAccess/Entities` needs an EF Core migration in
  `Crypter.DataAccess/Migrations`. Say so explicitly, and say whether it also needs a
  companion script in `Crypter.DataAccess/Scripts`.

### Scope

One pull request does one thing. Put drive-by refactors and cleanups under non-goals.

## 5. Settle it with the user

Show the user the plan and wait. This is the gate; the pipeline runs unattended once it opens.

Ask when two readings give materially different work. Decide the routine calls yourself and
say which way you went. Revise the plan in place until the user approves it.

## 6. Hand it to the pipeline

The mount is present on containers created from the current
`.devcontainer/docker-compose.yml`. Confirm the plan is visible, then start the run:

```bash
docker exec crypter-pipeline test -f /plans/{run-id}/plan.md
docker exec -w /work/Crypter crypter-pipeline \
  claude --dangerously-skip-permissions -p "/pipeline {run-id} {branch}"
```

Recreate the container to pick up the mount:

```bash
docker compose -f .devcontainer/docker-compose.yml up -d --force-recreate
```

Report what the pipeline returns: the pull request URL, whether its checks are green, and
anything it flagged.
