#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PACKAGES_FILE="${REPO_ROOT}/Directory.Packages.props"

EXPECTED_AI="2.23.0"
EXPECTED_WORKER_SERVICE="2.23.0"
EXPECTED_SERILOG_SINK_AI="5.0.0"

SERVICES_HOST="${REPO_ROOT}/FileIt.Module.Services/FileIt.Module.Services.Host/FileIt.Module.Services.Host.csproj"
SIMPLE_HOST="${REPO_ROOT}/FileIt.Module.SimpleFlow/FileIt.Module.SimpleFlow.Host/FileIt.Module.SimpleFlow.Host.csproj"

fail() {
  echo "ERROR: $1" >&2
  exit 1
}

require_tool() {
  command -v "$1" >/dev/null 2>&1 || fail "Required tool '$1' is not installed."
}

get_central_version() {
  local package_name="$1"
  awk -v RS=">" -v pkg="${package_name}" '
    $0 ~ /<PackageVersion/ && $0 ~ "Include=\"" pkg "\"" {
      if (match($0, /Version="[^"]+"/)) {
        version = substr($0, RSTART + 9, RLENGTH - 10)
        print version
        exit
      }
    }
  ' "${PACKAGES_FILE}"
}

assert_version() {
  local package_name="$1"
  local expected="$2"
  local actual
  actual="$(get_central_version "${package_name}")"
  [[ -n "${actual}" ]] || fail "Package '${package_name}' is missing from ${PACKAGES_FILE}."
  if [[ "${actual}" != "${expected}" ]]; then
    fail "${package_name} expected ${expected} in central package versions, found ${actual}."
  fi
}

assert_resolved_contains() {
  local project_file="$1"
  local package_name="$2"
  local expected="$3"
  local report
  report="$(dotnet list "${project_file}" package --include-transitive)"

  if ! grep -E "[[:space:]]> ${package_name}[[:space:]].*${expected}([[:space:]]|$)" <<<"${report}" >/dev/null; then
    fail "${package_name} did not resolve to ${expected} for ${project_file}."
  fi
}

require_tool dotnet
require_tool grep
require_tool sed

assert_version "Microsoft.ApplicationInsights" "${EXPECTED_AI}"
assert_version "Microsoft.ApplicationInsights.WorkerService" "${EXPECTED_WORKER_SERVICE}"
assert_version "Serilog.Sinks.ApplicationInsights" "${EXPECTED_SERILOG_SINK_AI}"

assert_resolved_contains "${SERVICES_HOST}" "Microsoft.ApplicationInsights" "${EXPECTED_AI}"
assert_resolved_contains "${SERVICES_HOST}" "Microsoft.ApplicationInsights.WorkerService" "${EXPECTED_WORKER_SERVICE}"
assert_resolved_contains "${SIMPLE_HOST}" "Microsoft.ApplicationInsights" "${EXPECTED_AI}"
assert_resolved_contains "${SIMPLE_HOST}" "Microsoft.ApplicationInsights.WorkerService" "${EXPECTED_WORKER_SERVICE}"

echo "Telemetry package guard passed."
