// SPDX-License-Identifier: GPL-2.0-only
namespace Oblikovati.Exporter.Inventor.Model
{
    /// <summary>
    /// A work plane captured as a frozen frame: its solved origin and two in-plane axes (the
    /// adapter derives the axes from the Inventor plane's normal). Lengths are centimetres
    /// (Inventor's database unit). This maps to a fixed-frame work plane, which carries the
    /// datum faithfully without re-deriving its Inventor construction.
    /// </summary>
    public sealed class InventorWorkPlane
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Plane origin in model space (cm).</summary>
        public double[] Origin { get; set; } = { 0, 0, 0 };

        /// <summary>In-plane X axis (unit vector).</summary>
        public double[] XAxis { get; set; } = { 1, 0, 0 };

        /// <summary>In-plane Y axis (unit vector).</summary>
        public double[] YAxis { get; set; } = { 0, 1, 0 };
    }
}
