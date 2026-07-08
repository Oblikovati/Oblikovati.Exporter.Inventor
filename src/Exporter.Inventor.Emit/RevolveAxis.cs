// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Maps a revolve's IR axis (a sketch centerline) to the bridge's <c>axisRef</c> string. The
    /// live revolve tool revolves about a WORK AXIS reference — an origin axis ("origin/axis/x|y|z")
    /// or a user axis — NOT a sketch line (unlike the recipe path's axisSketch/axisLine). There is
    /// no bridge tool to build a work axis from a sketch line, so this slice supports the common
    /// turned-part case: a centerline that, mapped through the sketch frame into model space,
    /// coincides with a global origin axis. Any other axis (parallel-but-offset, tilted) returns
    /// false so the caller defers that revolve rather than emit one about the wrong axis
    /// (correctness over coverage).
    /// </summary>
    public static class RevolveAxis
    {
        private const double Tolerance = 1e-6;

        /// <summary>Resolves the revolve's axis to an origin-axis ref; false ⇒ defer the revolve.</summary>
        public static bool TryResolve(InventorSketch sketch, InventorRevolve revolve, out string axisRef)
        {
            axisRef = string.Empty;
            if (sketch == null) throw new ArgumentNullException(nameof(sketch));
            if (revolve == null) throw new ArgumentNullException(nameof(revolve));
            if (!TryAxisCurve(sketch, revolve, out InventorCurve axis))
                return false;
            return TryOriginAxis(sketch, axis, out axisRef);
        }

        // Selects THIS revolve's axis line: the injected centerline at AxisLineIndex (the index among
        // the sketch's line-kind curves, matching the reader's Lines() order) when several revolves
        // share a sketch, else the sketch's single centerline. Ambiguous/absent ⇒ false.
        private static bool TryAxisCurve(InventorSketch sketch, InventorRevolve revolve, out InventorCurve axis)
        {
            axis = null!;
            List<InventorCurve> lines = LineCurves(sketch);
            if (revolve.AxisLineIndex >= 0 && revolve.AxisLineIndex < lines.Count)
            {
                axis = lines[revolve.AxisLineIndex];
                return true;
            }
            return TrySingleCenterline(sketch, out axis);
        }

        private static List<InventorCurve> LineCurves(InventorSketch sketch)
        {
            var lines = new List<InventorCurve>();
            foreach (InventorCurve c in sketch.Curves)
            {
                if (c.Kind == InventorCurveKind.Line)
                    lines.Add(c);
            }
            return lines;
        }

        private static bool TrySingleCenterline(InventorSketch sketch, out InventorCurve axis)
        {
            axis = null!;
            int count = 0;
            foreach (InventorCurve c in sketch.Curves)
            {
                if (!c.Centerline) continue;
                axis = c;
                count++;
            }
            return count == 1;
        }

        /// <summary>
        /// Classifies the centerline as a global origin axis: maps its 2D endpoints through the
        /// sketch frame into model space and, when the resulting line is parallel to a global axis
        /// AND passes through the global origin, returns that axis ref. Sign is irrelevant to a
        /// revolve axis, so ±X both map to "origin/axis/x".
        /// </summary>
        public static bool TryOriginAxis(InventorSketch sketch, InventorCurve axis, out string axisRef)
        {
            axisRef = string.Empty;
            double[] p0 = ToModel(sketch, axis.Start);
            double[] p1 = ToModel(sketch, axis.End);
            if (!TryDirection(p0, p1, out double[] dir))
                return false;
            int a = DominantAxis(dir);
            if (a < 0 || !OriginOnLine(p0, dir))
                return false;
            axisRef = "origin/axis/" + "xyz"[a];
            return true;
        }

        // p(x,y) = Origin + x·XAxis + y·YAxis — the sketch's 2D point in model space (cm).
        private static double[] ToModel(InventorSketch sketch, double[] p)
        {
            double[] o = sketch.Origin, x = sketch.XAxis, y = sketch.YAxis;
            return new[]
            {
                o[0] + p[0] * x[0] + p[1] * y[0],
                o[1] + p[0] * x[1] + p[1] * y[1],
                o[2] + p[0] * x[2] + p[1] * y[2],
            };
        }

        private static bool TryDirection(double[] a, double[] b, out double[] dir)
        {
            dir = new[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
            double len = Math.Sqrt(dir[0] * dir[0] + dir[1] * dir[1] + dir[2] * dir[2]);
            if (len < Tolerance)
                return false;
            dir[0] /= len; dir[1] /= len; dir[2] /= len;
            return true;
        }

        // The index of the global axis a unit direction lies along (|component| ≈ 1, others ≈ 0),
        // or -1 when the direction is off-axis.
        private static int DominantAxis(double[] dir)
        {
            for (int k = 0; k < 3; k++)
            {
                if (Math.Abs(Math.Abs(dir[k]) - 1.0) < Tolerance &&
                    Math.Abs(dir[(k + 1) % 3]) < Tolerance && Math.Abs(dir[(k + 2) % 3]) < Tolerance)
                    return k;
            }
            return -1;
        }

        // The global origin lies on the line through p with unit direction dir iff the component of
        // p perpendicular to dir vanishes: |p − (p·dir)dir| ≈ 0.
        private static bool OriginOnLine(double[] p, double[] dir)
        {
            double dot = p[0] * dir[0] + p[1] * dir[1] + p[2] * dir[2];
            double px = p[0] - dot * dir[0];
            double py = p[1] - dot * dir[1];
            double pz = p[2] - dot * dir[2];
            return Math.Sqrt(px * px + py * py + pz * pz) < Tolerance;
        }
    }
}
