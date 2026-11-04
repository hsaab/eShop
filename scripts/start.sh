#!/usr/bin/env bash
# One-command local start for the eShop Aspire AppHost.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK not found. Install .NET from https://dot.net then retry." >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker not found. Install Docker Desktop, then retry." >&2
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker is not running. Trying to start Docker Desktop..."
  if [[ "$(uname -s)" == "Darwin" ]]; then
    open -a Docker 2>/dev/null || open -a Docker.app 2>/dev/null || true
  fi
  for _ in $(seq 1 60); do
    if docker info >/dev/null 2>&1; then
      break
    fi
    sleep 2
  done
  if ! docker info >/dev/null 2>&1; then
    echo "Docker did not become ready. Start Docker Desktop manually, then run: npm start" >&2
    exit 1
  fi
  echo "Docker is ready."
fi

echo "Starting eShop (Aspire AppHost)..."
echo "  Shop:      https://localhost:7298"
echo "  Dashboard: printed in the log as Login to the dashboard at: ..."
echo

exec dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj "$@"
