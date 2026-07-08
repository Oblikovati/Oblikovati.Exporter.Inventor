// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Globalization;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Pure translation of Model IR feature enums/quantities into the string forms the bridge's
    /// extrude schema expects: <c>operation</c> new|join|cut|intersect, <c>extent</c>
    /// distance|through-all|to-next, <c>direction</c> positive|negative|symmetric, and the
    /// unit-bearing <c>distance</c>/<c>taper</c> expressions. Lengths in the IR are centimetres
    /// (Inventor's database unit); distances are emitted in millimetres to match the schema's
    /// documented "50 mm" form.
    /// </summary>
    public static class FeatureMapping
    {
        /// <summary>Maps a boolean operation to its extrude-schema spelling.</summary>
        public static string Operation(InventorOperation op)
        {
            switch (op)
            {
                case InventorOperation.NewBody: return "new";
                case InventorOperation.Join: return "join";
                case InventorOperation.Cut: return "cut";
                case InventorOperation.Intersect: return "intersect";
                default: throw new ArgumentOutOfRangeException(nameof(op), op, "unknown operation");
            }
        }

        /// <summary>Maps an extent kind to its extrude-schema spelling.</summary>
        public static string Extent(InventorExtentKind kind)
        {
            switch (kind)
            {
                case InventorExtentKind.Distance: return "distance";
                case InventorExtentKind.ThroughAll: return "through-all";
                case InventorExtentKind.ToNext: return "to-next";
                case InventorExtentKind.ToFace: return "to-face";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "unknown extent kind");
            }
        }

        /// <summary>Maps a growth direction to its extrude-schema spelling.</summary>
        public static string Direction(InventorExtentDirection dir)
        {
            switch (dir)
            {
                case InventorExtentDirection.Positive: return "positive";
                case InventorExtentDirection.Negative: return "negative";
                case InventorExtentDirection.Symmetric: return "symmetric";
                default: throw new ArgumentOutOfRangeException(nameof(dir), dir, "unknown direction");
            }
        }

        /// <summary>Formats a length in centimetres as a millimetre expression, e.g. 5 ⇒ "50 mm".</summary>
        public static string Millimeters(double centimetres) =>
            Invariant(centimetres * 10.0) + " mm";

        /// <summary>Formats a draft angle in radians as a degree expression, e.g. π/60 ⇒ "3 deg".</summary>
        public static string Degrees(double radians) =>
            Invariant(radians * 180.0 / Math.PI) + " deg";

        /// <summary>
        /// Maps a revolve's swept angle (radians) to its degree expression. A 0 angle — or a full
        /// 2π — is a full revolution and maps to "360 deg" (the IR encodes a full revolve as 0, and
        /// an extractor may equally hand back 2π; both mean the same solid).
        /// </summary>
        public static string RevolveAngle(double radians)
        {
            const double twoPi = 2.0 * Math.PI;
            if (Math.Abs(radians) < 1e-9 || Math.Abs(Math.Abs(radians) - twoPi) < 1e-9)
                return "360 deg";
            return Degrees(radians);
        }

        // Trims trailing zeros so 50 renders "50", 58.42920367 renders in full; invariant so a
        // comma-locale runner never emits "50,5 mm" (the host parses invariant-decimal).
        private static string Invariant(double value) =>
            value.ToString("0.############", CultureInfo.InvariantCulture);
    }
}
