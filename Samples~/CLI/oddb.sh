#!/usr/bin/env sh
set -eu
project=''
previous=''
for arg in "$@"; do
  if [ "$previous" = '--project' ]; then project=$arg; break; fi
  previous=$arg
done
if [ -z "$project" ]; then
  echo 'Pass --project with the Unity project root.' >&2
  exit 2
fi
project=$(cd "$project" && pwd)
for package in "$project/Assets/Plugins/ODDB" "$project/Packages/com.team-odd.oddb" "$project"/Library/PackageCache/com.team-odd.oddb@*; do
  if [ -f "$package/Tools~/CLI/dist/ODDB.Cli.dll" ]; then
    exec dotnet "$package/Tools~/CLI/dist/ODDB.Cli.dll" "$@"
  fi
done
echo 'ODDB CLI package source was not found in this Unity project.' >&2
exit 2
