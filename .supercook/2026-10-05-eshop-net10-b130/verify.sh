#!/usr/bin/env bash
# Verification recipe for the eShop net8 to net10 slice.
# The existing test suite is the behavior proof. This script does not add a test
# that hard-codes a framework version.
# Run from anywhere. The recipe lives two directories under the repository root.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "${repo_root}"

if [[ ! -f eShop.Web.slnf ]]; then
  echo "eShop.Web.slnf is not in ${repo_root}" >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is missing or not 10.0.x" >&2
  exit 1
fi

sdk_version="$(dotnet --version)"
if [[ ! "${sdk_version}" =~ ^10\.0\.[0-9]+ ]]; then
  echo "dotnet is ${sdk_version}, expected 10.0.x" >&2
  exit 1
fi

# Identity.API libman restore fetches jquery and bootstrap from cdnjs and unpkg.
# Those hosts fail TLS on this agent (LIB002). The property is a recipe flag, not a product change.
libman_flag=(-p:LibraryRestore=false)

dotnet restore eShop.Web.slnf "${libman_flag[@]}"
dotnet build eShop.Web.slnf "${libman_flag[@]}"

if docker info >/dev/null 2>&1; then
  dotnet test eShop.Web.slnf "${libman_flag[@]}"
else
  set +e
  dotnet test eShop.Web.slnf --filter "FullyQualifiedName!~FunctionalTests" "${libman_flag[@]}"
  test_status=$?
  set -e
  printf '%s\n' "Functional tests were not run because Docker is missing."
  if [[ "${test_status}" -ne 0 ]]; then
    exit "${test_status}"
  fi
fi

if ! command -v rg >/dev/null 2>&1; then
  echo "rg is missing, so leftover net8.0 targets cannot be checked" >&2
  exit 1
fi

set +e
rg -n "net8\\.0" -g "*.csproj" -g "global.json"
rg_status=$?
set -e
if [[ "${rg_status}" -eq 0 ]]; then
  echo "net8.0 remains in a csproj or global.json" >&2
  exit 1
fi
if [[ "${rg_status}" -ne 1 ]]; then
  echo "rg failed with status ${rg_status}" >&2
  exit "${rg_status}"
fi

if [[ -e src/eShop.AppHost/AppHost.cs ]]; then
  echo "src/eShop.AppHost/AppHost.cs exists" >&2
  exit 1
fi

if [[ ! -f src/eShop.AppHost/Program.cs ]]; then
  echo "src/eShop.AppHost/Program.cs is missing" >&2
  exit 1
fi

diff_out="$(git diff release/8.0 -- README.md .github Directory.Build.props)"
if [[ -n "${diff_out}" ]]; then
  echo "README.md, .github, or Directory.Build.props differs from release/8.0" >&2
  printf '%s\n' "${diff_out}" >&2
  exit 1
fi

echo "git rev-parse release/8.0"
release_sha="$(git rev-parse release/8.0)"
printf '%s\n' "${release_sha}"
if [[ "${release_sha}" != "f2369529433374a01b864b6fa1499ad894756f53" ]]; then
  echo "release/8.0 is ${release_sha}, expected f2369529433374a01b864b6fa1499ad894756f53" >&2
  exit 1
fi
