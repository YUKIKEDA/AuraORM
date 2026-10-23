#!/usr/bin/env bash
# Writes AURA_TEST_<NAME> and AURA_TEST_REQUIRE for one database of tools/databases.json to $GITHUB_ENV, with the
# container port of the job's service, so the connection string has one source for CI and tools/db.ps1.
# Usage: .github/scripts/integration-env.sh <name>
set -euo pipefail

name="$1"
entry="$(jq -c --arg name "$name" '.databases[] | select(.name == $name)' tools/databases.json)"
[[ -n "$entry" ]] || { echo "integration-env: no database named $name in tools/databases.json" >&2; exit 1; }

port="$(jq -r '.port' <<< "$entry")"
connection="$(jq -r '.connectionString' <<< "$entry" | sed "s/{port}/$port/")"
upper="$(tr '[:lower:]' '[:upper:]' <<< "$name")"

{
    echo "AURA_TEST_${upper}=${connection}"
    echo "AURA_TEST_REQUIRE=${name}"
} >> "$GITHUB_ENV"
