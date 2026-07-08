// SPDX-License-Identifier: GPL-2.0-only

using System;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// The <c>create_work_plane</c> arguments that reproduce a sketch's datum plane. This slice
    /// uses one construction mode — <c>fixed-frame</c> — because a sketch carries its plane as a
    /// frozen frame (solved origin + two in-plane axes), and the fixed-frame constructor takes
    /// exactly that (<c>AddFixed(origin, x, y)</c>), reproducing the datum's position AND
    /// orientation without re-deriving it from references. An offset-from-origin-plane mode would
    /// only cover axis-aligned parallels and needs base/sign detection — added fragility for no
    /// correctness gain, since fixed-frame already carries every datum faithfully.
    /// </summary>
    public readonly struct WorkPlaneSpec
    {
        public WorkPlaneSpec(double[] origin, double[] xAxis, double[] yAxis)
        {
            Origin = origin;
            XAxis = xAxis;
            YAxis = yAxis;
        }

        /// <summary>The <c>create_work_plane</c> kind this spec builds (always fixed-frame here).</summary>
        public string Kind => WorkPlaneMapper.FixedFrameKind;

        /// <summary>Plane origin in model space (cm), the fixed-frame <c>origin</c> argument.</summary>
        public double[] Origin { get; }

        /// <summary>In-plane X axis (unit vector), the fixed-frame <c>xaxis</c> argument.</summary>
        public double[] XAxis { get; }

        /// <summary>In-plane Y axis (unit vector), the fixed-frame <c>yaxis</c> argument.</summary>
        public double[] YAxis { get; }
    }

    /// <summary>
    /// Chooses the <c>create_work_plane</c> construction for a sketch on a non-origin datum. Maps a
    /// valid orthonormal sketch frame to a <see cref="WorkPlaneSpec"/> (fixed-frame); a degenerate
    /// or non-orthonormal frame (which the fixed-frame constructor would reject or distort) returns
    /// false so the caller defers that sketch — correctness over coverage.
    /// </summary>
    public static class WorkPlaneMapper
    {
        /// <summary>The create_work_plane kind for a frozen origin+X/Y datum (types.WorkPlaneFixed).</summary>
        public const string FixedFrameKind = "fixed-frame";

        private const double Tolerance = 1e-6;

        /// <summary>Maps a sketch's datum frame to a fixed-frame work-plane spec; false ⇒ defer.</summary>
        public static bool TryMap(InventorSketch sketch, out WorkPlaneSpec spec)
        {
            spec = default;
            if (sketch == null)
                throw new ArgumentNullException(nameof(sketch));
            if (!IsPoint3(sketch.Origin) || !IsUnit(sketch.XAxis) || !IsUnit(sketch.YAxis))
                return false;
            if (!IsOrthogonal(sketch.XAxis, sketch.YAxis))
                return false;
            spec = new WorkPlaneSpec(sketch.Origin, sketch.XAxis, sketch.YAxis);
            return true;
        }

        private static bool IsPoint3(double[] v) => v != null && v.Length == 3;

        private static bool IsUnit(double[] v) =>
            IsPoint3(v) && Math.Abs(Length(v) - 1.0) < Tolerance;

        private static bool IsOrthogonal(double[] a, double[] b) =>
            Math.Abs(a[0] * b[0] + a[1] * b[1] + a[2] * b[2]) < Tolerance;

        private static double Length(double[] v) =>
            Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
    }
}
