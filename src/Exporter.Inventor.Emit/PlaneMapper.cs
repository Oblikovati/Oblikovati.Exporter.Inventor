// SPDX-License-Identifier: GPL-2.0-only

using System;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Maps an <see cref="InventorSketch"/>'s plane onto the bridge's origin-plane name
    /// ("XY"|"XZ"|"YZ") that <c>create_sketch</c> accepts. This slice only handles the three
    /// origin planes; a sketch on an offset/tilted work plane returns false so the caller can
    /// defer it (work-plane sketches are a later slice).
    /// </summary>
    /// <example>
    /// <code>
    /// if (PlaneMapper.TryMapOriginPlane(sketch, out string plane)) { /* plane == "XY" */ }
    /// </code>
    /// </example>
    public static class PlaneMapper
    {
        private const double Tolerance = 1e-6;

        /// <summary>Maps a sketch on an axis-aligned origin plane to its name; false ⇒ defer.</summary>
        public static bool TryMapOriginPlane(InventorSketch sketch, out string plane)
        {
            plane = string.Empty;
            if (sketch == null)
                throw new ArgumentNullException(nameof(sketch));
            if (!IsOrigin(sketch.Origin))
                return false;
            if (!TryAxis(sketch.XAxis, out Axis x) || !TryAxis(sketch.YAxis, out Axis y))
                return false;
            return TryFrame(x, y, out plane);
        }

        // The three canonical origin frames Oblikovati names XY/XZ/YZ (in-plane X then Y).
        private static bool TryFrame(Axis x, Axis y, out string plane)
        {
            plane = string.Empty;
            if (x == Axis.PlusX && y == Axis.PlusY) plane = "XY";
            else if (x == Axis.PlusX && y == Axis.PlusZ) plane = "XZ";
            else if (x == Axis.PlusY && y == Axis.PlusZ) plane = "YZ";
            else return false;
            return true;
        }

        private static bool IsOrigin(double[] origin) =>
            origin != null && origin.Length == 3 &&
            Math.Abs(origin[0]) < Tolerance && Math.Abs(origin[1]) < Tolerance && Math.Abs(origin[2]) < Tolerance;

        private enum Axis { None, PlusX, PlusY, PlusZ }

        // Classifies a unit vector as one of the positive global axes; anything else (negative
        // or off-axis) is unhandled in this slice.
        private static bool TryAxis(double[] v, out Axis axis)
        {
            axis = Axis.None;
            if (v == null || v.Length != 3)
                return false;
            if (IsUnit(v[0]) && IsZero(v[1]) && IsZero(v[2])) axis = Axis.PlusX;
            else if (IsZero(v[0]) && IsUnit(v[1]) && IsZero(v[2])) axis = Axis.PlusY;
            else if (IsZero(v[0]) && IsZero(v[1]) && IsUnit(v[2])) axis = Axis.PlusZ;
            else return false;
            return true;
        }

        private static bool IsUnit(double c) => Math.Abs(c - 1.0) < Tolerance;

        private static bool IsZero(double c) => Math.Abs(c) < Tolerance;
    }
}
