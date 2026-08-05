---
name: crypter-step-plan
description: Draft an implementation plan for a change to Crypter, interactively. Invoked as /crypter-step-plan "<requirement>" [output-path] by the crypter-change skill, and usable on its own when a plan is all you want.
---

# Crypter step plan

You turn a requirement into a plan someone else implements from. They see the plan and nothing
else — not your reasoning, not the files you read, not the alternatives you rejected. Write for
that reader.

The web, the user's tooling, and the user are available to you. Settle anything that needs them
here, and write the answer into the plan.

A plan stands on its own. Writing one commits you to nothing: the plan is worth having whether
it goes to `crypter-change`, to a person, or nowhere.

## 1. Sync

```bash
git fetch upstream
git fetch origin
```

Read the code at `upstream/stable`, the commit a build branches from.

## 2. Understand before deciding

Read `CLAUDE.md` and `Documentation/Development/Coding Standard.md` first. Then read the code
the requirement touches, and the code around it — the existing patterns are the ones the
implementation matches.

Prefer reusing what exists. If a monad, primitive, service, or extension already does most of
the job, name it in the plan with its path.

## 3. Write the plan

Write it to the output path you were given. Absent one, use
`.claude/plans/{short-name}/plan.md`, taking a short name from the requirement —
`transfer-limits`, `fix-expiry-tz`. Create the directory as needed.

- **Goal** — one paragraph. What changes for a user of Crypter, and why.
- **Non-goals** — what this change deliberately leaves alone. Be specific; this keeps the
  implementer in scope, and the conformance auditor checks against it.
- **Approach** — the design, in prose. Name the types and methods to add or change. Explain
  anything non-obvious, especially where a constraint forced the shape.
- **Steps** — numbered and ordered, each naming the files it touches. A step should be small
  enough that its result is obvious.
- **Tests** — what to add to `Crypter.Test` or `Crypter.Test.Web` and what each case pins down.
  CI is where the suite runs, so tests are what verify behaviour.
- **Risks** — what could break, and what a reviewer should look at hardest.

### Crypter's idioms are part of the plan

Express the plan in the conventions the code already uses, so the implementer inherits them:

- `Maybe<T>` and `Either<TLeft, TRight>` from `Crypter.Common/Monads` for expected failures.
- Validated types from `Crypter.Common/Primitives` rather than raw strings.
- `Async` suffix on async methods, and async all the way for database, file, and network IO.
- Constructors over object initializers. Enums over magic strings.
- Any change to an entity under `Crypter.DataAccess/Entities` needs an EF Core migration in
  `Crypter.DataAccess/Migrations`. Say so explicitly, and say whether it also needs a companion
  script in `Crypter.DataAccess/Scripts`.

### Scope

One pull request does one thing. Put drive-by refactors and cleanups under non-goals.

## 4. Settle it with the user

Show the user the plan and wait.

Ask when two readings give materially different work. Being able to ask is why this runs on the
host; use it. Decide the routine calls yourself and say which way you went. Revise the plan in
place until the user approves it.

Report the path you wrote and what the user settled.
