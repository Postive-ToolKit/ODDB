#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if ! command -v pwsh >/dev/null 2>&1; then
    echo "PowerShell 7 (pwsh) is required for the shared Core build and provenance checks." >&2
    exit 1
fi
exec pwsh -NoProfile -File "$SCRIPT_DIR/build.ps1" \
    -Configuration "${1:-Release}" -CoreProjectPath "${ODDB_CORE_PROJECT_PATH:-}"
