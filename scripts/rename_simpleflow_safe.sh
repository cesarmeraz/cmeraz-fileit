#!/usr/bin/env bash
set -euo pipefail
OLD="FileIt.Module.SimpleFlow"
NEW="FileIt.Module.SimpleFlow"
BRANCH="refactor/module/simpleflow/rename"
SLN="FileIt.All.sln"
git checkout -b "$BRANCH"
if [ ! -d "$OLD" ]; then
  echo "Source folder '$OLD' not found; aborting." >&2
  exit 1
fi
if [ -d "$NEW" ]; then
  for entry in "$OLD"/*; do
    [ -e "$entry" ] || continue
    name=$(basename "$entry")
    git mv "$entry" "$NEW/$name"
  done
  rmdir "$OLD" || true
else
  git mv "$OLD" "$NEW"
fi
git commit -m "chore(rename): move $OLD -> $NEW" || true
find "$NEW" -type f -name '*.csproj' | while read -r f; do
  base=$(basename "$f")
  if echo "$base" | grep -q "$OLD"; then
    newbase="${base/$OLD/$NEW}"
    git mv "$f" "$(dirname "$f")/$newbase"
  fi
done
git commit -m "chore(rename): rename csproj filenames under $NEW" || true
git ls-files "$NEW/**" -z | xargs -0 sed -i "s/${OLD}/${NEW}/g" || true
git add -A "$NEW"
git commit -m "refactor(namespaces): update $OLD -> $NEW in sources" || true
git ls-files '*.csproj' -z | xargs -0 sed -i "s/${OLD}/${NEW}/g" || true
git add -A
git commit -m "chore(projectrefs): fix project references" || true
grep -oE '[^"]*FileIt\.SimpleFlow[^"]*\.csproj' "$SLN" || true | while read -r oldproj; do
  [ -n "$oldproj" ] && dotnet sln "$SLN" remove "$oldproj" || true
done
find "$NEW" -name '*.csproj' -print0 | xargs -0 -n1 dotnet sln "$SLN" add || true
git add "$SLN"
git commit -m "chore(sln): update $SLN for $NEW" || true
dotnet build --configuration Debug
dotnet test --configuration Debug
echo "Done. Push branch: git push -u origin $BRANCH"
