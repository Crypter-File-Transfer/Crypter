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
  echo "usage: crypter-workspace create {run-id} [refspec]" >&2
  echo "       crypter-workspace remove {run-id}" >&2
  exit 64
}

subcommand="${1:-}"
run_id="${2:-}"
[[ -n "${subcommand}" && -n "${run_id}" ]] || usage

# The run id becomes a path under /work that `remove` deletes recursively, so it has to be a
# plain name before it is used as one.
if [[ ! "${run_id}" =~ ^[A-Za-z0-9][A-Za-z0-9._-]*$ ]]; then
  echo "Run id '${run_id}' is not a plain name" >&2
  exit 64
fi

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

    # --no-hardlinks because the mount is read-only and owned by another uid, which is exactly
    # the case where git's hardlink optimisation is unavailable. Copying is predictable.
    git clone --quiet --no-hardlinks "${host_git}" "${workspace}"

    # The agents diff against upstream/stable. Take it from the host's own remote-tracking ref
    # so it reflects the org repository rather than whatever branch the host has checked out.
    git -C "${workspace}" fetch --quiet origin \
      '+refs/remotes/origin/stable:refs/remotes/upstream/stable'

    if [[ -n "${3:-}" ]]; then
      git -C "${workspace}" fetch --quiet origin "${3}"
    fi

    git -C "${workspace}" checkout --quiet -B stable refs/remotes/upstream/stable

    git -C "${workspace}" config user.name "${CRYPTER_GIT_NAME}"
    git -C "${workspace}" config user.email "${CRYPTER_GIT_EMAIL}"

    echo "Workspace ready at ${workspace}"
    git -C "${workspace}" log --oneline -1 refs/remotes/upstream/stable
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
