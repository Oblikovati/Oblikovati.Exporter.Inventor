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

# Goldens whose solid volume (cm³) is asserted against the real reader, with a relative
# tolerance per file (default 0.5%; a revolved tube reads ~0.6% under analytic 24π because
# physicalProperties measures the faceted body, so it gets a wider band).
declare -A EXPECT_VOL=(
    [box.opd]=60
    [revolve.opd]=75.398          # washer: 24π (R=4, r=2, h=2)
    [rect-pattern.opd]=180         # 3 × 60
    [mirror.opd]=120               # 2 × 60
    [circular-pattern.opd]=240     # 4 × 60
    [filleted-box.opd]=59.73       # 60 − 0.5²(1−π/4)·5 (edge descriptor bound, ADR-0040)
    [chamfered-box.opd]=59.375     # 60 − 0.5²/2·5
    [shelled-box.opd]=33           # 60 − 3×2×4.5 cavity (top face descriptor bound)
    [holed-box.opd]=58.43          # 60 − π·0.5²·2 (placement-face descriptor bound)
    [sweep.opd]=31.42              # π·1²·10 cylinder (circle profile swept along +Z)
    [loft.opd]=31.42               # π·1²·10 cylinder (loft between two coaxial circles)
    [arc-extrude.opd]=31.42        # 10π half-cylinder (half-disc with an arc, extruded)
)
# Curved-surface results read the faceted body, so they get a wider band.
declare -A EXPECT_TOL=(
    [revolve.opd]=0.02 [filleted-box.opd]=0.02 [holed-box.opd]=0.02 [sweep.opd]=0.02 [loft.opd]=0.02
    [arc-extrude.opd]=0.02
)
# Goldens validated by the open check only (no DOF assertion): an ellipse cannot be fully
# constrained — Oblikovati has no ellipse radius dimension to pin its radii (as with arc radii).
declare -A OPEN_ONLY=( [ellipse.opd]=1 )

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
    if [[ "$f" == *.oad ]] || [ -n "${OPEN_ONLY[$name]:-}" ]; then
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
        tol="${EXPECT_TOL[$name]:-0.005}"
        got="$("$CLI" script run "$ROOT/scripts/volume.lua" --doc "$f" 2>/dev/null | tr -d '[:space:]')"
        if awk -v g="$got" -v w="$want" -v t="$tol" 'BEGIN { d = g - w; if (d < 0) d = -d; exit !(g != "" && d <= w * t) }'; then
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
