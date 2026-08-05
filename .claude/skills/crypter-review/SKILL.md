---
name: crypter-review
description: Review an existing pull request with the container's reviewer lenses and report what they found. Use when asked to scrutinise a pull request, or invoked as /crypter-review {pr-number}.
---

# Crypter review

Put an existing pull request through the same lenses a change of your own goes through.

Use it on a pull request that deserves more scrutiny than a read, and on pull requests other
people raised. The lenses run in the container, against a copy of the pull request fetched into
its clone.

The findings come back to the user. Nothing is posted to GitHub.

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

## 4. Report

Read the files and tell the user what is in them:

- What each lens raised, and which findings you would act on first.
- Where a finding rests on an assumption about intent, say so — the lenses read a diff, not a
  discussion.
- Which findings you checked against the code yourself and stand behind, separately from those
  you are relaying.
- Where the artifacts are.

Say plainly where the lenses found nothing. A quiet review is a result.

Posting any of this to the pull request is the user's call, and theirs to do.
