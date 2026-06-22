#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-2.0-only
#
# Downloads the official Autodesk Inventor Interop binary (Autodesk.Inventor.Interop.dll) for
# a given Inventor major version into a destination directory, for a real-assembly build.
# Autodesk publishes these for add-in developers at:
#   https://www.autodesk.com/support/technical/article/caas/tsarticles/ts/1r0objlzvLJeSDzxNV8b87.html
# The DLL is Autodesk's IP — it is fetched transiently to compile against and is NEVER
# committed to this repo (the adapter references it with Private=false; Inventor supplies it
# at runtime).
#
# Usage: scripts/fetch-interop.sh <interop-version> <dest-dir>
#   e.g. scripts/fetch-interop.sh v31.0 /tmp/inventor-2027
set -euo pipefail

VER="${1:?usage: fetch-interop.sh <interop-version e.g. v31.0> <dest-dir>}"
DEST="${2:?usage: fetch-interop.sh <interop-version> <dest-dir>}"
URL="https://damassets.autodesk.net/content/dam/autodesk/www/files/${VER}.zip"
UA="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36"
REF="https://www.autodesk.com/support/technical/article/caas/tsarticles/ts/1r0objlzvLJeSDzxNV8b87.html"

mkdir -p "$DEST"
tmp="$(mktemp -d)"
curl -fsSL -A "$UA" -H "Accept: application/zip,*/*" -H "Referer: $REF" \
    --retry 3 --max-time 120 -o "$tmp/${VER}.zip" "$URL"
unzip -o -q "$tmp/${VER}.zip" -d "$tmp"

dll="$(find "$tmp" -iname 'Autodesk.Inventor.Interop.dll' | head -1)"
[ -n "$dll" ] || { echo "fetch-interop: no Autodesk.Inventor.Interop.dll in ${VER}.zip" >&2; exit 1; }
cp "$dll" "$DEST/Autodesk.Inventor.Interop.dll"
echo "fetched $VER -> $DEST/Autodesk.Inventor.Interop.dll ($(du -h "$DEST/Autodesk.Inventor.Interop.dll" | cut -f1))"
