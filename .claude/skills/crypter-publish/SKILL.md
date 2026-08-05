---
name: crypter-publish
description: Take a branch the pipeline built in the container, push it to the fork, open a pull request, and hold it against CI. Use when the pipeline has finished, or invoked as /crypter-publish {run-id} {branch}.
---

# Crypter publish

Take the branch the pipeline built and turn it into a pull request with green checks.

**This runs on the host.** The container holds no credential, so every authenticated GitHub
operation happens here, with yours.

You are given a run id and a branch name: `/crypter-publish {run-id} {branch}`.

## 1. Fetch the branch out of the container

The branch lives in the container's clone. `git` reaches it over `docker exec`:

```bash
git -c protocol.ext.allow=user fetch \
  "ext::docker exec -i crypter-pipeline git upload-pack /work/Crypter" {branch}:{branch}
```

`protocol.ext.allow` is passed per command and stays out of your config. **If this fails, stop
and say so** — the branch is the whole deliverable.

## 2. Push to the fork

```bash
git fetch upstream
git push origin upstream/stable:refs/heads/stable
git push -u origin {branch}
```

The first push keeps the fork's `stable` level with the org repository, so the pull request
compares against current code.

## 3. Open the pull request

Open it against the fork, base `stable`, as a draft, using whatever GitHub access this session
has — the `gh` CLI, or the GitHub MCP server's `create_pull_request`.

Take the title and description from the pipeline's report. Write the description for the org
repository's reviewers, since it carries over when the upstream pull request is opened.

## 4. Hold it against CI

Invoke `ci-watcher` with the pull request number, the attempt number, and
`.claude/plans/{run-id}/ci-{n}.md`. It runs **one attempt**: it reads the checks for the head
commit, watches them, and reports.

You own the loop:

1. `ci-watcher` reports green → go to stage 5.
2. `ci-watcher` reports a failure → run the fix in the container with the report it wrote:

   ```bash
   docker exec -w /work/Crypter crypter-pipeline \
     claude --dangerously-skip-permissions -p "/pipeline-fix {run-id} {branch} /plans/{run-id}/ci-{n}.md"
   ```

   Then fetch the new commits as in stage 1, push them, and invoke `ci-watcher` again with the
   next attempt number.
3. **Stop after three attempts.** Comment the state of play on the pull request and hand back
   to the user.

Stop earlier and ask the user whenever another attempt looks pointless — the same check failing
the same way twice, a failure the plan did not anticipate, or anything that reads as a wrong
plan rather than wrong code. Three attempts is the ceiling, not a quota to spend.

Stop immediately, without spending an attempt, if `ci-watcher` reports that no run appeared for
the commit. Workflows stay disabled on a new fork until they are enabled once in its Actions
tab, and that is a setup problem.

## 5. Report

Tell the user:

- The fork pull request URL and whether its checks are green. It is a draft; taking it out of
  draft is theirs.
- What each fix attempt changed, if any ran.
- Anything the pipeline could not do, and what it rejected in triage.
- If the loop gave up: which check failed and what the last attempt tried.

The upstream pull request is a separate one against `Crypter-File-Transfer/Crypter`, since the
base repository is fixed when a pull request is created. The description is ready to paste.
