---
name: crypter-triage-review
description: Verify the findings left on a pull request, push back on the ones that do not hold, and fix the ones that do. Use when asked to work through review comments, or invoked as /crypter-triage-review {pr-number}.
---

# Crypter triage review

Work through the findings on a pull request. Each one is either answered on the thread or
recorded as verified, and the verified ones become commits.

Findings come from anywhere — the reviewer lenses, a person, another tool. They are treated the
same way, because where a finding came from says nothing about whether it is true.

Run from the root of a checkout. `/plans` and `/runs` resolve against it, and the container is
named after it, so the one you reach is always the one whose artifacts you are reading.

## Setup

You are given a pull request number: `/crypter-triage-review {pr-number}`.

Use `pr-{number}` as the run id.

```bash
mkdir -p .claude/runs/pr-{number}/verification
chmod 777 .claude/runs/pr-{number} .claude/runs/pr-{number}/verification
```

The container's `agent` is uid 1001 and your files are uid 1000, so the agents write into
directories this side creates and grants.

Then confirm the container is up and current, before anything depends on it:

```bash
.devcontainer/pipeline.sh exec -- test -w /runs/pr-{number}/verification && \
  .devcontainer/pipeline.sh exec -- test -x /usr/local/bin/crypter-workspace
```

`pipeline.sh` resolves the container from the checkout it sits in, so which container you get is
settled by where you are rather than by anything you check. What the probes are for is the rest:
the first proves `/runs` is mounted and that uid 1001 can write the directory you just made, and
the second proves the image carries the current tooling. Without them a later step fails in a way
that reads like something else — a missing executable, verification written somewhere you never
look.

Both are `test` because `docker exec` runs a binary and not a shell, so a builtin like
`command -v` exits 127 whether or not the thing it was looking for is there.

**Do not `docker start` an exited container to fix this.** The image is fixed when a container is
created, so starting an old one brings back the old tooling. Bring it up from here instead:

```bash
.devcontainer/pipeline.sh up
```

That rebuilds the image and recreates this checkout's container. It cannot touch another
checkout's. What it does destroy is every workspace under `/work` in *this* container, because
`/work` is the container's own filesystem and not a volume — so **ask the user before running it
if another run may be live here.**

## 1. Collect the findings

Read the pull request with whatever GitHub access this session has — the `gh` CLI, or the GitHub
MCP server's `pull_request_read` with `get_review_comments`, `get_reviews` and `get_comments`.

Take the head branch and head repository from `get` while you are there. You need both later.

Write every open finding to `.claude/runs/pr-{number}/review.md`, one entry each:

- A short id you assign, `f1` upward.
- The thread or comment id, so a reply can find its way back.
- The file and line, where it has one.
- The finding, quoted in full.

Skip threads already resolved and comments that raise nothing — approvals, thanks, questions
about intent. A question is for the author to answer, not for a verifier.

**If there is nothing open, say so and stop.**

## 2. Fetch the head into a workspace

The container clones from the repository itself, so the head comes straight from GitHub and
nothing has to be staged in your checkout first:

```bash
.devcontainer/pipeline.sh exec -- crypter-workspace create pr-{number} \
  '+refs/pull/{number}/head:refs/heads/{head-branch}'
```

The branch in the workspace takes the pull request's own branch name, so the commits go back to
the branch they came from.

**If it fails, stop and say so.**

## 3. Verify

```bash
.devcontainer/pipeline.sh exec -w /work/pr-{number} -- \
  claude --permission-mode auto -p "/crypter-devcontainer-verify pr-{number} {head-branch} /runs/pr-{number}/review.md"
```

Verdicts land in `.claude/runs/pr-{number}/verification/{id}.md`. Read the files, not the
summary.

## 4. Answer each finding

Every finding gets one of three outcomes, and none of them is silence.

**Does not hold** — reply on the thread with `add_reply_to_pull_request_comment`. Where the
finding has no thread to reply on, comment on the pull request itself with `add_issue_comment`,
quoting enough of the finding that the reply stands on its own.

Give the evidence: what the code does instead,
by file and line. Two or three sentences. Say it as a position, not a verdict — the person who
raised it may know something the verifier could not see, and the thread is where that comes out.

Reply once. If they answer, that is the user's conversation, not yours to continue.

**Holds** — write it to `.claude/runs/pr-{number}/triage.md`: the id, the failure, and the file
and line. That file is what the fix is built from, so write it for someone who has not read the
thread.

**Unsettled** — carry it to the user in your report. Do not reply, and do not fix.

## 5. Fix what held

Where `triage.md` has anything, and the head branch is one you can push to:

```bash
.devcontainer/pipeline.sh exec -w /work/pr-{number} -- \
  claude --permission-mode auto -p "/crypter-devcontainer-remediate pr-{number} {head-branch} /runs/pr-{number}/triage.md"
```

Then invoke `crypter-step-open-pull-request` with the run id and the head branch. It pushes the
commits and leaves the existing pull request in place.

A pull request from a repository you cannot push to stops here. The replies stand, `triage.md`
stands, and the author does the fixing. Say so in the report.

## 6. Tear down and report

```bash
.devcontainer/pipeline.sh exec -- crypter-workspace remove pr-{number}
```

Remove it on every exit path, including the ones where you stopped early. The verdicts under
`.claude/runs/pr-{number}` are the record and they stay.

Then report:

- What held, what did not, and what you could not settle.
- The replies you posted, and where.
- What changed on the branch, and the commits now on the pull request.
- Where the artifacts are.

CI is not watched here. A push starts a round of checks; reading them is `/crypter-change`'s job
or yours.
