#!/usr/bin/env bash
# Resolve which pipeline container belongs to this checkout, and drive its lifecycle.
#
# /plans and /runs are relative mounts, so a container is welded to the checkout it was created
# from. Naming the container after that checkout is what lets several exist at once: the name a
# checkout resolves to is its own, so an orchestrator can never reach another checkout's mounts.
#
# The name is derived rather than configured. There is nothing to set, and nothing that can drift
# out of step with where the checkout actually is.
#
# Parallel runs within one checkout need none of this. They are already separated by the per-run
# workspaces at /work/{run-id}.
set -euo pipefail

script_dir="$(cd "$(dirname "$(realpath "${BASH_SOURCE[0]}")")" && pwd)"
checkout="$(dirname "${script_dir}")"
compose_file="${script_dir}/docker-compose.yml"

# Shared by every instance on the machine, which is why Claude Code is authenticated once and the
# package caches are warmed once. Declared external in the Compose file, so nothing creates them
# but this script.
volumes=(crypter-pipeline-claude crypter-pipeline-nuget crypter-pipeline-pnpm)

usage() {
  cat >&2 <<'EOF'
usage: pipeline.sh name                     print this checkout's container name
       pipeline.sh exec [flags] -- {cmd}    run a command in this checkout's container
       pipeline.sh up                       create or recreate it, rebuilding the image
       pipeline.sh down                     stop and remove it
       pipeline.sh list                     every pipeline container, and its checkout
EOF
  exit 64
}

# The name is both a container name and a Compose project name. Compose is the stricter of the
# two: lowercase, and no dots. The slug keeps `docker ps` readable and the hash of the real path
# separates two checkouts that share a basename.
container_name() {
  local slug hash
  slug="$(basename "${checkout}" | tr '[:upper:]' '[:lower:]' | tr -c 'a-z0-9' '-')"
  slug="${slug#-}"
  slug="${slug%-}"
  slug="${slug:0:20}"
  slug="${slug:-checkout}"

  hash="$(printf '%s' "${checkout}" | sha256sum | cut -c1-8)"

  printf 'crypter-pipeline-%s-%s\n' "${slug}" "${hash}"
}

compose() {
  local name
  name="$(container_name)"
  CRYPTER_PIPELINE_CONTAINER="${name}" \
  CRYPTER_PIPELINE_CHECKOUT="${checkout}" \
    docker compose --project-name "${name}" --file "${compose_file}" "$@"
}

case "${1:-}" in
  name)
    container_name
    ;;

  exec)
    shift
    # Flags for docker exec come first, then `--`, then the command. The separator is what keeps
    # a command's own flags from being read as docker's.
    flags=()
    while [[ $# -gt 0 && "${1}" != "--" ]]; do
      flags+=("${1}")
      shift
    done
    [[ "${1:-}" == "--" ]] || usage
    shift
    [[ $# -gt 0 ]] || usage

    docker exec "${flags[@]}" "$(container_name)" "$@"
    ;;

  up)
    # Compose will not create an external volume, and a missing one fails the `up` rather than
    # being made on the fly. Creating is idempotent, so this is safe on every run.
    for volume in "${volumes[@]}"; do
      docker volume create "${volume}" >/dev/null
    done

    compose up --detach --build
    ;;

  down)
    compose down
    ;;

  list)
    docker ps --all \
      --filter 'label=com.crypter.pipeline.checkout' \
      --format 'table {{.Names}}\t{{.Status}}\t{{.Label "com.crypter.pipeline.checkout"}}'
    ;;

  *)
    usage
    ;;
esac
