#!/usr/bin/env bash
# Create and remove the per-run workspaces the agents build and review in.
#
# A workspace is a clone of the repository taken from CRYPTER_REPO_URL. It belongs to one run and
# is removed with it, so no copy of the repository outlives the state it was made from.
#
# Cloning from the remote rather than from the host means a run sees the branch as the repository
# holds it, not as some checkout happens to have fetched it. Nothing on the host is mounted here,
# so neither uncommitted work nor a stale checkout can reach a run.
#
# The clone is anonymous and read-only. Branches leave a workspace by the host fetching from it
# over `docker exec ... git upload-pack`, so no credential is needed in here.
set -euo pipefail

usage() {
  echo "usage: crypter-workspace create {run-id} [--base {branch}] [refspec]" >&2
  echo "       crypter-workspace remove {run-id}" >&2
  exit 64
}

subcommand="${1:-}"
run_id="${2:-}"
[[ -n "${subcommand}" && -n "${run_id}" ]] || usage
shift 2

# The run id becomes a path under /work that `remove` deletes recursively, so it has to be a
# plain name before it is used as one.
if [[ ! "${run_id}" =~ ^[A-Za-z0-9][A-Za-z0-9._-]*$ ]]; then
  echo "Run id '${run_id}' is not a plain name" >&2
  exit 64
fi

# The branch the run is built or reviewed against. A pull request states its own, so the caller
# passes what it read rather than letting this default stand in for it.
base="stable"
if [[ "${1:-}" == "--base" ]]; then
  base="${2:-}"
  [[ -n "${base}" ]] || usage
  shift 2
fi

# The base reaches git as a ref, where a leading dash would be read as an option instead.
if [[ ! "${base}" =~ ^[A-Za-z0-9][A-Za-z0-9._/-]*$ ]]; then
  echo "Base branch '${base}' is not a plain branch name" >&2
  exit 64
fi

refspec="${1:-}"
workspace="/work/${run_id}"

case "${subcommand}" in
  create)
    : "${CRYPTER_GIT_NAME:?Set CRYPTER_GIT_NAME in .devcontainer/.env to the author name on the commits}"
    : "${CRYPTER_GIT_EMAIL:?Set CRYPTER_GIT_EMAIL in .devcontainer/.env to the author email on the commits}"
    : "${CRYPTER_REPO_URL:?Set CRYPTER_REPO_URL to the repository the workspaces are cloned from}"

    if [[ -e "${workspace}" ]]; then
      echo "A workspace already exists at ${workspace}. Remove it or use another run id." >&2
      exit 1
    fi

    git clone --quiet "${CRYPTER_REPO_URL}" "${workspace}"

    # The base has to exist before the run is measured against it, and the clone is the first
    # place that can be checked. A workspace without its base is no use, so it goes.
    if ! git -C "${workspace}" rev-parse --verify --quiet "refs/remotes/origin/${base}" >/dev/null
    then
      echo "${CRYPTER_REPO_URL} has no ${base} branch." >&2
      rm -rf "${workspace}"
      exit 1
    fi

    if [[ -n "${refspec}" ]]; then
      git -C "${workspace}" fetch --quiet origin "${refspec}"
    fi

    git -C "${workspace}" checkout --quiet -B "${base}" "refs/remotes/origin/${base}"

    git -C "${workspace}" config user.name "${CRYPTER_GIT_NAME}"
    git -C "${workspace}" config user.email "${CRYPTER_GIT_EMAIL}"

    echo "Workspace ready at ${workspace}, based on ${base}"
    git -C "${workspace}" log --oneline -1 "refs/remotes/origin/${base}"
    ;;

  remove)
    if [[ ! -d "${workspace}" ]]; then
      echo "No workspace at ${workspace}"
      exit 0
    fi

    rm -rf "${workspace}"
    echo "Removed ${workspace}"
    ;;

  *)
    usage
    ;;
esac
