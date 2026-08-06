---
name: crypter-review
description: Review an existing pull request with the container's reviewer lenses, verify what they found, and post the findings that hold. Use when asked to scrutinise a pull request, or invoked as /crypter-review {pr-number}.
---

# Crypter review

Put an existing pull request through the same lenses a change of your own goes through.

Use it on a pull request that deserves more scrutiny than a read, and on pull requests other
people raised. The lenses run in the container, against a copy of the pull request fetched into
its clone.

Every finding lands on disk. The ones that survive verification also land on the pull request, as
one review that comments and neither approves nor requests changes.

The lenses do the reviewing and the verifiers rule on what they found. Both run in the container.
You start them, carry what survives to the pull request, and report the rest — **you never review
the diff yourself, and you never decide whether a finding is true.**

Everything that reaches the author came from a lens that read the code in the container and a
verifier that checked it there. A review posted from here is attributed to them, so anything of
your own inside it is a claim made in someone else's name.

## Setup

You are given a pull request number: `/crypter-review {pr-number}`.

Run from the root of the main checkout. The container's mounts resolve against it.

Use `pr-{number}` as the run id.

```bash
mkdir -p .claude/runs/pr-{number}/findings .claude/runs/pr-{number}/verification
chmod 777 .claude/runs/pr-{number} .claude/runs/pr-{number}/findings \
  .claude/runs/pr-{number}/verification
```

The container's `agent` is uid 1001 and your files are uid 1000, so the agents write into
directories this side creates and grants. Creating them here also keeps you able to delete what
they wrote.

Then confirm the running container is the one this checkout describes, before anything depends
on it:

```bash
docker exec crypter-pipeline test -w /runs/pr-{number}/findings && \
  docker exec crypter-pipeline test -w /runs/pr-{number}/verification && \
  docker exec crypter-pipeline test -x /usr/local/bin/crypter-workspace
```

The first proves the `/runs` mount reaches the directory you just made, which a container
created against a different checkout will not. The second proves the image carries the current
tooling. A container that fails either is not this checkout's, and every later step fails
against it in a way that reads like something else — a missing executable, findings written
somewhere you never look.

Both are `test` because `docker exec` runs a binary and not a shell, so a builtin like
`command -v` exits 127 whether or not the thing it was looking for is there.

**Do not `docker start` an exited container to fix this.** Mounts and image are fixed when a
container is created, so starting one built from another checkout, or from an older image,
brings back the same wrong container. Bring it up from here instead:

```bash
docker compose -f .devcontainer/docker-compose.yml up -d --build
```

That rebuilds the image and recreates the container against this checkout's mounts. It replaces
any container of the same name, so **ask the user before running it** — theirs may belong to
another checkout and hold work you cannot see.

## 1. Read the pull request

Read its title, description and diff with whatever GitHub access this session has — the `gh`
CLI, or the GitHub MCP server's `pull_request_read`. What the author says it does is context for
reading the diff, and worth carrying into your report where the two disagree.

Read for orientation and for the base branch, not for defects. This read is how you follow the
lenses later, not a first pass at the review. Whatever you notice here is not a finding, and
noticing it is not a reason to go looking for more.

Take its **base branch** from the same read — `base.ref` from `pull_request_read` with method
`get`, or `.baseRefName` from `gh pr view`. Most pull requests here target `stable`, but a
release targets `main`, and nothing about the number tells you which. Everything below diffs
against the branch the pull request actually names.

## 2. Fetch it into a workspace

The container has no network remote. It clones from your repository through a read-only mount,
so the pull request head goes into your repository first and travels across from there:

```bash
git fetch origin +refs/pull/{number}/head:refs/pr/{number} {base-branch}
docker exec crypter-pipeline crypter-workspace create pr-{number} \
  --base {base-branch} '+refs/pr/{number}:refs/heads/pr-{number}'
```

The refspec is forced, so reviewing a pull request again after its author rebased or amended
picks up the new head instead of being rejected. The base branch is fetched alongside it because
the workspace clones your repository, and a base you have never fetched is not there to diff
against.

**If either fails, stop and say so.**

The workspace lasts for this review and no longer.

## 3. Examine

```bash
docker exec -w /work/pr-{number} crypter-pipeline \
  claude --permission-mode auto -p "/crypter-devcontainer-examine pr-{number} pr-{number} origin/{base-branch}"
```

No plan path. A pull request raised elsewhere has no plan to hold it against, so the plan
adherence phase sits out and the lenses do the work.

Findings land in `.claude/runs/pr-{number}/findings/{lens}.md`.

`claude -p` exits 0 whether or not the run worked. An unknown command, an expired session and a
clean review all come back as success, so the exit code tells you nothing. Look at what it wrote
instead:

```bash
ls .claude/runs/pr-{number}/findings/
```

An empty directory is a failed run, not four quiet lenses — a lens with nothing to say still
writes its file. **Stop and say so.**

Do not stand in for the lenses that did not run. Reading the diff and writing up what you would
have found produces a review with nothing behind it, wearing their name, at exactly the moment
there is nothing to post and the pull request looks unreviewed. The run failing is the result;
report that instead.

## 4. Verify

A lens that has already been wrong once will happily be wrong again, and a finding posted is a
finding the author has to answer. So every finding is ruled on against the code before it goes
anywhere — by a verifier in the container, one per finding, not by you.

```bash
docker exec -w /work/pr-{number} crypter-pipeline \
  claude --permission-mode auto -p "/crypter-devcontainer-verify pr-{number} pr-{number} /runs/pr-{number}/findings/"
```

Given the findings directory, verify treats every file in it as a lens report, assigns each
finding an id, and gives one verifier the finding and nothing else.

Verdicts land in `.claude/runs/pr-{number}/verification/{id}.md`, each with the evidence behind
it.

An empty `verification/` means one of two things and they are not the same. Either every lens
found nothing, which verify reports and which ends in no review — a real and welcome result — or
the run failed the silent way step 3 describes. Verify's own report distinguishes them. **If it
failed, stop and say so**; do not read the absence of verdicts as a clean diff.

**Do not read the findings before the verdicts exist.** A finding you have already formed a view
on is one you will post or bury on your own authority, which is the whole thing this step moves
into the container. Wait for the verdict and route by it.

## 5. Post the review

One review, event `COMMENT`. Never approve and never request changes — that is the user's, and
this pull request may not be theirs.

Use the GitHub MCP server's `pull_request_review_write` with method `create` to open a pending
review, `add_comment_to_pending_review` for each finding that names a file and a line **in the
diff**, then `submit_pending`.

**Only findings that held go up.** A finding the verifier ruled against is the process working,
and the pull request is not where that belongs — it would cost the author a read to reach the
same conclusion the verifier already reached with the code in front of it. Unsettled findings do
not go up either; nothing unverified reaches the author.

They are not lost. The verdicts and their evidence stay under `.claude/runs/pr-{number}/`, which
is where a later pass over this pipeline reads what the lenses claimed and how it turned out.

The review body carries:

- Which lenses ran, and which found nothing. A quiet lens is a result worth stating.
- Every held finding that has no line to hang on, in full.
- That the lenses read the diff rather than the discussion around it, so a finding resting on an
  assumption about intent says so.

Attribute it. The body opens by naming the lenses as its author and the verifiers as what ruled
on them, so the person reading knows what produced it. **Nothing in the review is yours.**

**If nothing held, post no review.** Say so to the user instead. A review that reports only that
it found nothing still costs everyone subscribed a notification.

A line comment that the API rejects for being outside the diff goes in the body instead. **Do not
retry it against a different line.**

## 6. Tear down and report

```bash
docker exec crypter-pipeline crypter-workspace remove pr-{number}
```

Remove it on every exit path, including the ones where you stopped early. The findings and
verdicts under `.claude/runs/pr-{number}` are the record and they stay.

Then report:

- The review URL, and what went up.
- The counts: how many findings each lens raised, how many held, how many did not, how many are
  unsettled.
- The unsettled ones in full. They reached nobody else, so this is the only place they surface.
- Which lenses found nothing.
- Where the artifacts are.

A lens that raised plenty and had none of it hold is worth a sentence of its own. That is the
pipeline telling you something about the lens rather than about the pull request.
