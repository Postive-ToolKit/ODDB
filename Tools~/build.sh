#!/usr/bin/env bash
# Rebuild ODDB.Core and drop the fresh dll into the Unity package.
#
# Run from anywhere — paths are resolved relative to this script's location.
# Requires .NET SDK (`brew install dotnet` or https://dotnet.microsoft.com).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CORE_PROJECT="$(cd "$PACKAGE_ROOT/../../../src/ODDB.Core" && pwd)"
PLUGINS_DIR="$PACKAGE_ROOT/Plugins"

CONFIG="${1:-Release}"
DLL="$CORE_PROJECT/bin/$CONFIG/netstandard2.1/ODDB.Core.dll"

echo "→ building ODDB.Core ($CONFIG)"
dotnet build "$CORE_PROJECT/ODDB.Core.csproj" -c "$CONFIG" --nologo -v quiet

if [[ ! -f "$DLL" ]]; then
    echo "ERROR: expected dll not found at $DLL" >&2
    exit 1
fi

mkdir -p "$PLUGINS_DIR"
cp "$DLL" "$PLUGINS_DIR/ODDB.Core.dll"
echo "→ copied $(basename "$DLL") → $PLUGINS_DIR/"
echo "   $(stat -f '%z bytes' "$PLUGINS_DIR/ODDB.Core.dll")"
echo "done. Unity will recompile on next focus."
