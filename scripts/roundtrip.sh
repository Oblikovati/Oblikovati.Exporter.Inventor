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

# Goldens whose solid volume (cm³) is asserted exactly against the real reader.
declare -A EXPECT_VOL=( [box.opd]=60 )

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
        continue
    fi
    if ! "$CLI" script run "$ROOT/scripts/validate_sketches.lua" --doc "$f" >/dev/null 2>&1; then
        echo "FAIL $name (sketch validation)"
        status=1
        continue
    fi
    # 3) For solid-producing goldens, assert the exact volume in the real reader.
    want="${EXPECT_VOL[$name]:-}"
    if [ -n "$want" ]; then
        got="$("$CLI" script run "$ROOT/scripts/volume.lua" --doc "$f" 2>/dev/null | tr -d '[:space:]')"
        if awk -v g="$got" -v w="$want" 'BEGIN { d = g - w; if (d < 0) d = -d; exit !(g != "" && d <= w * 0.001) }'; then
            echo "OK   $name (volume ${got} cm³)"
        else
            echo "FAIL $name (volume ${got} cm³, want ${want})"
            status=1
        fi
    else
        echo "OK   $name"
    fi
done

exit "$status"
