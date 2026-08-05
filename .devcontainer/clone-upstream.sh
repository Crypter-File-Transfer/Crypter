#!/usr/bin/env bash
# Prepare the pipeline workspace: a clone of the org repository. The container runs this on
# every start; an existing workspace is left alone.
#
# The workspace is a named volume rather than a bind mount of a host checkout. The agents get
# their own clone, so they cannot touch uncommitted work on the host. The clone is anonymous
# and the remote has no push url, so the agents read public code and commit locally. Pushing
# and opening pull requests happen on the host.
set -euo pipefail

: "${CRYPTER_GIT_NAME:?Set CRYPTER_GIT_NAME in .devcontainer/.env to the author name on the commits}"
: "${CRYPTER_GIT_EMAIL:?Set CRYPTER_GIT_EMAIL in .devcontainer/.env to the author email on the commits}"

upstream_repo="${CRYPTER_UPSTREAM:-Crypter-File-Transfer/Crypter}"

# Has to match the workspace path the container skills and docker-compose.yml use.
workspace="/work/Crypter"

git config --global user.name "${CRYPTER_GIT_NAME}"
git config --global user.email "${CRYPTER_GIT_EMAIL}"

if [[ -d "${workspace}/.git" ]]; then
  echo "Workspace already present at ${workspace}"
else
  git clone --origin upstream "https://github.com/${upstream_repo}.git" "${workspace}"
fi

# A push from the container fails here rather than at a credential prompt.
git -C "${workspace}" remote set-url --push upstream no-push

git -C "${workspace}" fetch --quiet upstream

echo "Workspace ready at ${workspace}"
git -C "${workspace}" remote -v
