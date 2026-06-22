#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-2.0-only
#
# Emits golden documents with the C# translator/writer and opens each with the real
# oblikovati-cli. This binds the exporter's emitter to the actual Oblikovati reader,
# catching schema drift. Used by CI Job 2 and runnable locally.
#
# Usage: scripts/roundtrip.sh <path-to-oblikovati-cli>
set -euo pipefail

CLI="${1:?usage: roundtrip.sh <path-to-oblikovati-cli>}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$(mktemp -d)"

dotnet run --project "$ROOT/tools/GoldenGen" -c Release -- "$OUT"

status=0
for f in "$OUT"/*.opd "$OUT"/*.oad; do
    [ -e "$f" ] || continue
    name="$(basename "$f")"
    # The file loads and recomputes in the real reader. Sketch/feature fidelity checks
    # (DOF 0, closed profiles) are added with the sketch milestone.
    if "$CLI" open "$f" >/dev/null; then
        echo "OK   $name"
    else
        echo "FAIL $name (open)"
        status=1
    fi
done

exit "$status"
