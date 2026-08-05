#!/usr/bin/env bash
# Watch a pull request's checks to a conclusion and, where they failed, print the part of the
# log that explains why.
#
# This is the mechanical half of watching CI: waiting, reading exit codes, finding the failing
# runs, and cutting the noise out of their logs. Deciding what the error means belongs to
# whoever reads the output.
#
# usage: ci-status.sh {pr-number} [owner/repo]
#
# Exit codes are the caller's signal and are deliberately distinct:
#   0  every check passed
#   1  a check failed; the log extract is on stdout
#   3  no checks have started for the head commit
#   4  gh is not authenticated
#   8  checks were still pending when the watch ended
set -uo pipefail

pr_number="${1:-}"
repo="${2:-}"

if [[ -z "${pr_number}" ]]; then
  echo "usage: ci-status.sh {pr-number} [owner/repo]" >&2
  exit 64
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "gh is not authenticated. Run 'gh auth login'." >&2
  exit 4
fi

gh_args=(--repo "${repo}")
[[ -z "${repo}" ]] && gh_args=()

head_sha=$(gh pr view "${pr_number}" "${gh_args[@]}" --json headRefOid --jq .headRefOid 2>/dev/null)
if [[ -z "${head_sha}" ]]; then
  echo "Could not read pull request ${pr_number}." >&2
  exit 64
fi

echo "Head commit: ${head_sha}"

checks=$(gh pr checks "${pr_number}" "${gh_args[@]}" --watch 2>&1)
watch_status=$?

# A pull request whose checks never started reports this rather than an empty table, and it
# means a setup problem rather than a slow queue.
if grep -qi "no checks reported" <<<"${checks}"; then
  echo "No checks have started for ${head_sha}."
  exit 3
fi

echo "${checks}"

case "${watch_status}" in
  0)
    echo
    echo "All checks passed."
    exit 0
    ;;
  8)
    echo
    echo "Checks still pending when the watch ended. Run again."
    exit 8
    ;;
esac

# Strip the "job<TAB>step<TAB>timestamp " prefix gh puts on every log line, which is most of the
# width and none of the information.
strip_prefix() {
  sed -E 's/^[^\t]*\t[^\t]*\t[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9:.]+Z //'
}

# The runner marks the failure with ##[error], but that line is the symptom — "the build
# failed". The cause sits above it, so print a window ending at the marker.
extract_failure() {
  local log="$1"
  local marker
  marker=$(grep -n '##\[error\]' "${log}" | head -1 | cut -d: -f1)

  if [[ -z "${marker}" ]]; then
    echo "  No ##[error] marker found. Last 40 lines:"
    tail -40 "${log}" | strip_prefix | sed 's/^/  /'
    return
  fi

  local from=$(( marker - 60 ))
  (( from < 1 )) && from=1
  sed -n "${from},$(( marker + 2 ))p" "${log}" | strip_prefix | sed 's/^/  /'
}

echo
echo "=== Failing runs for ${head_sha} ==="

failed_runs=$(gh run list --commit "${head_sha}" "${gh_args[@]}" \
  --status failure --json databaseId,workflowName --jq '.[] | "\(.databaseId)\t\(.workflowName)"')

if [[ -z "${failed_runs}" ]]; then
  echo "A check failed but no failing workflow run was found for the commit."
  echo "The failure may belong to a check that is not an Actions run."
  exit 1
fi

# The full logs are kept rather than cleaned up. The extract below is a window, and whoever
# reads it may need more; leaving the files behind means they open a file instead of going back
# to the network for a second copy.
log_dir=$(mktemp -d -t ci-status-XXXXXX)

while IFS=$'\t' read -r run_id workflow_name; do
  [[ -z "${run_id}" ]] && continue
  echo
  echo "--- ${workflow_name} (run ${run_id}) ---"
  echo "    https://github.com/${repo:-$(gh repo view --json nameWithOwner --jq .nameWithOwner)}/actions/runs/${run_id}"
  echo

  log="${log_dir}/${run_id}.log"
  if gh run view "${run_id}" "${gh_args[@]}" --log-failed >"${log}" 2>/dev/null && [[ -s "${log}" ]]; then
    extract_failure "${log}"
    echo
    echo "  Full log: ${log}"
  else
    echo "  Could not read the failed log for run ${run_id}."
  fi
done <<<"${failed_runs}"

exit 1
