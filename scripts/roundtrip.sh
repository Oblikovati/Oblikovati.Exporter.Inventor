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
    # 1) The file loads and recomputes in the real reader.
    if ! "$CLI" open "$f" >/dev/null; then
        echo "FAIL $name (open)"
        status=1
        continue
    fi
    # 2) For parts, every sketch is fully constrained (DOF 0) with a closed profile.
    #    Assemblies (.oad) have no part sketch context, so the open check suffices.
    if [[ "$f" == *.oad ]]; then
        echo "OK   $name"
    elif "$CLI" script run "$ROOT/scripts/validate_sketches.lua" --doc "$f" >/dev/null 2>&1; then
        echo "OK   $name"
    else
        echo "FAIL $name (sketch validation)"
        status=1
    fi
done

exit "$status"
