---
name: crypter-review
description: Review an existing pull request with the container's reviewer lenses and post what they found to the pull request. Use when asked to scrutinise a pull request, or invoked as /crypter-review {pr-number}.
---

# Crypter review

Put an existing pull request through the same lenses a change of your own goes through.

Use it on a pull request that deserves more scrutiny than a read, and on pull requests other
people raised. The lenses run in the container, against a copy of the pull request fetched into
its clone.

The findings land on disk and on the pull request, as one review that comments and neither
approves nor requests changes.

## Setup

You are given a pull request number: `/crypter-review {pr-number}`.

Run from the root of the main checkout. The container's mounts resolve against it.

Use `pr-{number}` as the run id.

```bash
mkdir -p .claude/runs/pr-{number}/findings
chmod 777 .claude/runs/pr-{number} .claude/runs/pr-{number}/findings
```

The container's `agent` is uid 1001 and your files are uid 1000, so the agents write into
directories this side creates and grants. Creating them here also keeps you able to delete what
they wrote.

## 1. Read the pull request

Read its title, description and diff with whatever GitHub access this session has — the `gh`
CLI, or the GitHub MCP server's `pull_request_read`. What the author says it does is context for
reading the diff, and worth carrying into your report where the two disagree.

## 2. Fetch it into the container

Pull request heads are public refs on the org repository, so the container reaches them
anonymously:

```bash
docker exec crypter-pipeline \
  git -C /work/Crypter fetch upstream +pull/{number}/head:pr-{number}
```

The refspec is forced, so reviewing a pull request again after its author rebased or amended
picks up the new head instead of being rejected.

**If this fails, stop and say so.**

## 3. Examine

```bash
docker exec -w /work/Crypter crypter-pipeline \
  claude --permission-mode auto -p "/crypter-examine pr-{number} pr-{number}"
```

No plan path. A pull request raised elsewhere has no plan to hold it against, so the plan
adherence phase sits out and the lenses do the work.

Findings land in `.claude/runs/pr-{number}/findings/{lens}.md`.

## 4. Triage

Read every finding against the code before you carry it to the pull request. A lens that has
already been wrong once will happily be wrong again, and a finding posted is a finding the author
has to answer.

Keep anything with a concrete failure behind it. Drop preferences, restatements of what the
author already chose, and findings about code the diff did not touch.

Write what you kept and what you dropped, with a reason for each, to
`.claude/runs/pr-{number}/triage.md`. That file is how the user checks this judgement, and it is
what a later remediation run reads.

## 5. Post the review

One review, event `COMMENT`. Never approve and never request changes — that is the user's, and
this pull request may not be theirs.

Use the GitHub MCP server's `pull_request_review_write` with method `create` to open a pending
review, `add_comment_to_pending_review` for each finding that names a file and a line **in the
diff**, then `submit_pending`. Where `gh` is installed, `gh pr review --comment` posts the body.

The review body carries:

- Which lenses ran, and which found nothing. A quiet lens is a result worth stating.
- Every finding you kept that has no line to hang on, in full.
- That the lenses read the diff rather than the discussion around it, so a finding resting on an
  assumption about intent says so.

Attribute it. The body opens by naming the lenses as its author, so the person reading knows what
produced it.

A line comment that the API rejects for being outside the diff goes in the body instead. **Do not
retry it against a different line.**

## 6. Report

Tell the user the review URL, what you kept and dropped, which findings you checked against the
code yourself and stand behind, and where the artifacts are.
