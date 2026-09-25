#!/usr/bin/env sh
set -eu
project=''
previous=''
for arg in "$@"; do
  if [ "$previous" = '--project' ]; then project=$arg; break; fi
  previous=$arg
done
if [ -n "$project" ]; then
  project=$(cd "$project" && pwd)
else
  cursor=$(cd "$(dirname "$0")" && pwd)
  while :; do
    if [ -d "$cursor/Assets" ] && [ -d "$cursor/Packages" ]; then
      project=$cursor
      break
    fi
    parent=$(dirname "$cursor")
    [ "$parent" = "$cursor" ] && break
    cursor=$parent
  done
  if [ -z "$project" ]; then
    echo 'Cannot locate the Unity project; pass --project with its root.' >&2
    exit 2
  fi
fi
for package in "$project/Assets/Plugins/ODDB" "$project/Packages/com.team-odd.oddb" "$project"/Library/PackageCache/com.team-odd.oddb@*; do
  if [ -f "$package/Tools~/CLI/dist/ODDB.Cli.dll" ]; then
    exec dotnet "$package/Tools~/CLI/dist/ODDB.Cli.dll" "$@"
  fi
done
echo 'ODDB CLI package source was not found in this Unity project.' >&2
exit 2
