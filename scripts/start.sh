#!/usr/bin/env bash
# One-command local start for the eShop Aspire AppHost.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SHOP_URL="https://localhost:7298"
SHOP_HTTP_URL="http://localhost:5045"
DASHBOARD_URL="https://localhost:19888"

port_listening() {
  local port="$1"
  if command -v lsof >/dev/null 2>&1; then
    lsof -nP -iTCP:"$port" -sTCP:LISTEN >/dev/null 2>&1
    return $?
  fi
  # Fallback when lsof is missing (rare on macOS/Linux with nc).
  if command -v nc >/dev/null 2>&1; then
    nc -z 127.0.0.1 "$port" >/dev/null 2>&1
    return $?
  fi
  return 1
}

print_urls() {
  echo "Shop:      $SHOP_URL"
  echo "           $SHOP_HTTP_URL"
  echo "Dashboard: $DASHBOARD_URL"
  echo "           (Aspire login URL with token is also printed in the AppHost log)"
}

# Already running: do not start a second AppHost (ports 19888/7298/19076 collide).
if port_listening 7298 || port_listening 19888; then
  echo "eShop looks like it is already running."
  print_urls
  if command -v curl >/dev/null 2>&1; then
    code="$(curl -sk -o /dev/null -w '%{http_code}' "$SHOP_URL/" 2>/dev/null || true)"
    if [[ "$code" == "200" || "$code" == "302" || "$code" == "307" ]]; then
      echo "Shop check: HTTP $code"
      exit 0
    fi
  fi
  echo "Ports are in use but the shop did not respond. Stop the old AppHost (Ctrl+C in its terminal, or kill the eShop.AppHost / dcp process), then run npm start again."
  exit 1
fi

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
print_urls
echo

exec dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj "$@"
