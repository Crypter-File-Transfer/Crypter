#!/usr/bin/env bash
# Create and remove the per-run workspaces the agents build and review in.
#
# A workspace is a clone of the host repository taken from the read-only /host-git mount. It
# belongs to one run and is removed with it, so no copy of the repository outlives the state it
# was made from.
#
# The mount is read-only, so the container reads committed history and writes nothing back. The
# host's working tree is not mounted at all, which is what keeps uncommitted work invisible here.
set -euo pipefail

host_git="/host-git"

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

    if [[ ! -d "${host_git}" ]]; then
      echo "No host repository at ${host_git}. The container was started without its mount." >&2
      exit 1
    fi

    if [[ -e "${workspace}" ]]; then
      echo "A workspace already exists at ${workspace}. Remove it or use another run id." >&2
      exit 1
    fi

    # Resolve the base before anything is created, so a base that is not there leaves nothing
    # behind to remove first.
    if ! git -C "${host_git}" rev-parse --verify --quiet "refs/remotes/origin/${base}" >/dev/null
    then
      echo "The host repository has no origin/${base}. Fetch it there and try again." >&2
      exit 1
    fi

    # --no-hardlinks because the mount is read-only and owned by another uid, which is exactly
    # the case where git's hardlink optimisation is unavailable. Copying is predictable.
    git clone --quiet --no-hardlinks "${host_git}" "${workspace}"

    # A clone maps the source's local branches into origin/*, so origin/stable here would mean
    # whatever the host has checked out rather than what the org repository holds. Point the
    # remote at the host's own remote-tracking refs instead, so origin/{branch} means the same
    # thing in a workspace as it does on the host. Configuring the refspec rather than fetching
    # it once keeps a later bare `git fetch` from putting the host's local branches back.
    git -C "${workspace}" config remote.origin.fetch \
      '+refs/remotes/origin/*:refs/remotes/origin/*'
    git -C "${workspace}" fetch --quiet --prune origin

    if [[ -n "${refspec}" ]]; then
      git -C "${workspace}" fetch --quiet origin "${refspec}"
    fi

    # The clone takes origin/HEAD from the host's checked-out branch, which is the one thing in
    # the origin namespace that would still mean the host rather than the org. Point it at the
    # base, so a bare `origin` resolves to what the run is measured against.
    git -C "${workspace}" remote set-head origin "${base}"

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
