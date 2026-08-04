---
name: pipeline
description: Take a requirement from plan to an open, CI-green draft pull request on the fork, using a chain of subagents. Use when asked to run the pipeline on a requirement, or invoked as /pipeline "<requirement>".
---

# Pipeline

Turn a requirement into a draft pull request whose checks pass, in stages, each run by a
subagent with its own context. A later stage that starts fresh actually re-examines the work;
one that inherits the reasoning behind it rubber-stamps it.

**This runs inside the devcontainer.** `origin` is the fork, `upstream` is the org repository
and is read-only. Every pull request is fork → fork. Nothing here can reach
`Crypter-File-Transfer/Crypter`, and the upstream pull request is something the user opens by
hand at the end, from a fork pull request they have read.

There is exactly one stop: the user approves the plan. Everything after that runs to a draft
pull request with green checks, or to a written account of why CI would not take it.

## Setup

Pick a short run id from the requirement — `transfer-limits`, `fix-expiry-tz`. Then:

```bash
mkdir -p /work/Crypter/.claude/pipeline/{run-id}
```

State goes there: `plan.md`, `conformance.md`, `findings/`, `ci.md`. It is gitignored.

## 0. Sync and branch

Never plan against stale code:

```bash
git -C /work/Crypter fetch upstream
git -C /work/Crypter fetch origin
git -C /work/Crypter push origin upstream/stable:refs/heads/stable
git -C /work/Crypter worktree add /work/Crypter/.claude/worktrees/{run-id} -b {branch} upstream/stable
```

**If any of these fail, stop and say so.** A quietly skipped sync means the plan, the diff, and
the eventual upstream pull request are all built on the wrong base, and nothing downstream will
notice.

Name the branch as the repo does: `feature/{something}`, `fix/{something}`, `chore/{something}`.

Every later stage gets this worktree path and works by absolute path inside it. Never `cd`.

## 1. Plan

Invoke `plan-author` with the requirement verbatim, the worktree path, and the output path
`/work/Crypter/.claude/pipeline/{run-id}/plan.md`.

Then **stop.** Show the user the plan — the file, not a summary of it — and wait. Do not
implement, do not create the pull request, do not start reviewing. If they ask for changes, run
`plan-author` again with their feedback and the existing plan; do not edit the plan yourself.

If the plan contains an **Open question**, put it in front of the user explicitly. It is the
one thing they are most likely to want to change and the cheapest moment to change it.

## 2. Implement

Invoke `implementer` with the plan path and the worktree path. Give it nothing about how the
plan was reached — the plan is the specification.

Read its report. If it says a step could not be done, that is not a failure to paper over:
surface it to the user with the rest of the results at the end, and let the auditor record it.

## 3. Open the draft pull request

Now, once there is something real to look at and before anyone reviews it. You do the pushing,
here and at every later stage — the implementer commits and returns:

```bash
git -C /work/Crypter/.claude/worktrees/{run-id} push -u origin {branch}
gh pr create --repo {fork} --draft --base stable --head {branch} --title "..." --body "..."
```

Creating the pull request starts the first round of checks. Checks run on drafts, so the round
begins here rather than at stage 7, and every push after this one starts another. Nothing
cancels the round it supersedes, so push once per stage, after the implementer is done.

Title reads like a commit subject: imperative, capitalized, no trailing period.

Description is a few sentences of plain English saying what changed and why. **Do not argue the
case** — no justifying the approach, no pre-empting objections, no listing rejected
alternatives. Call out what a reviewer would otherwise have to discover: migrations, breaking
API changes, deliberately held-back dependencies. That is information, not argument.

This description carries over verbatim when the user opens the upstream pull request, so write
it for the org repository's reviewers.

## 4. Examine

Run these in parallel — they do not interact:

- `conformance-auditor` with the plan, the worktree, and
  `/work/Crypter/.claude/pipeline/{run-id}/conformance.md`.
- `reviewer`, once per lens, with the worktree and
  `/work/Crypter/.claude/pipeline/{run-id}/findings/{lens}.md`.

The lens list is currently one entry:

| Lens | Brief |
|---|---|
| general | Correctness and edge cases first, then scope creep, then the conventions in `CLAUDE.md`. |

Adding lenses later — security, simplicity, test coverage — means adding rows here. The
`reviewer` definition does not change; the lens comes from the prompt.

## 5. Triage

You decide what to act on. Read every finding against the code before accepting it — a reviewer
that has already been wrong once will happily be wrong again, and acting on a bad finding means
changing working code.

Accept anything with a concrete failure behind it. Reject preferences, restatements of the plan
you already chose against, and findings about code the diff did not touch. An unplanned extra
that contradicts the plan's non-goals is not a preference — accept it.

Write what you accepted and what you rejected, with a reason for each rejection, into
`/work/Crypter/.claude/pipeline/{run-id}/findings/triage.md`. The user reads this to check your
judgement.

## 6. Remediate

If anything was accepted, invoke `implementer` with the accepted findings and the worktree
path. Each fix is its own commit on the existing branch. When it returns, push once; that
updates the same draft pull request and starts a fresh round of checks.

If nothing was accepted, go straight to stage 7 — the round of checks from the last push is
the one that counts.

## 7. Hold it against CI

Invoke `ci-watcher` with the worktree path, the pull request number, the attempt number, and
`/work/Crypter/.claude/pipeline/{run-id}/ci.md`. It runs **one attempt**: it finds the round of
checks for the branch's current commit, watches it, and reports.

You own the loop:

1. `ci-watcher` reports green → go to step 8.
2. `ci-watcher` reports a failure → invoke `implementer` with that failure report and the
   worktree path, push, then invoke `ci-watcher` again with the next attempt number.
3. **Stop after three attempts.** Comment the state of play on the pull request, and hand back
   to the user. Three failures on the same change usually means the plan was wrong, not the
   code, and a fourth attempt buys a full build and test suite for nothing.

A fresh `ci-watcher` per attempt is deliberate — it reads what CI actually says now, rather than
reasoning from its own previous guess about the failure.

Stop immediately, without spending attempts, if `ci-watcher` reports that no run ever appeared
for the commit. Workflows are disabled on a new fork until they are enabled once in its Actions
tab, and that is a setup problem.

## 8. Hand off

```bash
git -C /work/Crypter worktree remove /work/Crypter/.claude/worktrees/{run-id}
```

Remove it on every exit path, including when the pipeline stopped early.

Then tell the user, in a few sentences:

- The fork pull request URL and whether its checks are green. It is still a draft; taking it
  out of draft is theirs to do once they have read it.
- Anything the implementer could not do, and any deviation the auditor flagged as drift.
- What you rejected in triage that they might disagree with.
- If the CI loop gave up: which check failed and what the last attempt tried.

They open the upstream pull request themselves. Remind them the base repository is fixed when a
pull request is created, so it is a new pull request against
`Crypter-File-Transfer/Crypter` — the description is ready to paste.
