#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-2.0-only
#
# Builds and packages the add-in for one Inventor version into an installable zip: the .addin
# manifest plus a bin/ folder with the add-in assembly, its dependencies, and the matching
# Autodesk.Inventor.Interop.dll (an Inventor 2025+ add-in must ship the interop alongside it).
#
# Usage: scripts/package.sh <inventor-year> <version> <out-zip>
#   e.g. scripts/package.sh 2027 0.1.0 Oblikovati.Exporter.Inventor-0.1.0-2027.zip
set -euo pipefail

YEAR="${1:?usage: package.sh <inventor-year> <version> <out-zip>}"
VERSION="${2:?usage: package.sh <inventor-year> <version> <out-zip>}"
OUTZIP="${3:?usage: package.sh <inventor-year> <version> <out-zip>}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INTEROP="$ROOT/interop/$YEAR/Autodesk.Inventor.Interop.dll"
[ -f "$INTEROP" ] || { echo "package: no vendored interop for Inventor $YEAR ($INTEROP)" >&2; exit 1; }

# Resolve the output path to absolute BEFORE we cd into the staging dir (otherwise the zip
# lands in the staging dir and includes itself).
case "$OUTZIP" in /*) ABSZIP="$OUTZIP" ;; *) ABSZIP="$PWD/$OUTZIP" ;; esac

STAGE="$(mktemp -d)"
mkdir -p "$STAGE/bin"

# Publish the add-in against the real interop. Private=false means the interop is not copied,
# so we add the matching one explicitly below.
dotnet publish "$ROOT/src/Exporter.Inventor.Entry" -c Release \
    -p:UseInventorStubs=false -p:InventorSdkDir="$ROOT/interop/$YEAR" \
    -p:Version="$VERSION" -o "$STAGE/bin" >/dev/null

cp "$INTEROP" "$STAGE/bin/"
cp "$ROOT/deploy/Oblikovati.Exporter.Inventor.addin" "$STAGE/"
cp "$ROOT/deploy/README.md" "$STAGE/"

rm -f "$ABSZIP"
(cd "$STAGE" && zip -rq "$ABSZIP" .)
echo "packaged Inventor $YEAR v$VERSION -> $ABSZIP"
