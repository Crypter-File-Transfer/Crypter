#!/usr/bin/env bash
# Prepare the pipeline workspace: a clone of your fork, with the org repository added as a
# read-only upstream. The devcontainer runs this once, when the container is created.
#
# The workspace is a named volume rather than a bind mount of a host checkout. The agents
# get their own clone, so they cannot touch uncommitted work on the host, and `origin` is
# the fork that the container's fork-scoped token can actually push to.
set -euo pipefail

: "${CRYPTER_FORK:?Set CRYPTER_FORK on the host to <owner>/<repo> of your fork}"
: "${GH_TOKEN:?Set CRYPTER_FORK_TOKEN on the host so it reaches the container as GH_TOKEN}"

upstream_repo="${CRYPTER_UPSTREAM:-Crypter-File-Transfer/Crypter}"
workspace="${CRYPTER_WORKSPACE:-/work/Crypter}"

if [[ "${CRYPTER_FORK}" == "${upstream_repo}" ]]; then
  echo "CRYPTER_FORK is the upstream repository. Point it at your fork instead." >&2
  exit 1
fi

git config --global user.name "${CRYPTER_GIT_NAME:-Crypter pipeline}"
git config --global user.email "${CRYPTER_GIT_EMAIL:-pipeline@users.noreply.github.com}"
gh auth setup-git

if [[ -d "${workspace}/.git" ]]; then
  echo "Workspace already present at ${workspace}"
else
  git clone "https://github.com/${CRYPTER_FORK}.git" "${workspace}"
fi

if ! git -C "${workspace}" remote get-url upstream >/dev/null 2>&1; then
  git -C "${workspace}" remote add upstream "https://github.com/${upstream_repo}.git"
fi

git -C "${workspace}" fetch --quiet origin
git -C "${workspace}" fetch --quiet upstream

echo "Workspace ready at ${workspace}"
git -C "${workspace}" remote -v
