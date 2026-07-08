// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>
    /// Builds an <see cref="OccurrenceData"/> from an Inventor occurrence and the file name its
    /// component was exported to. The placement becomes a 16-cell row-major transform with the
    /// rotation in the upper-left 3x3 and the translation in cells 3, 7, 11 — the layout
    /// math.Matrix4 uses. Inventor's database unit is centimetres, so the position passes through.
    /// </summary>
    public static class OccurrenceTranslator
    {
        public static OccurrenceData Translate(InventorOccurrence occurrence, string componentFileName) =>
            new OccurrenceData
            {
                Name = occurrence.Name,
                Component = componentFileName,
                Transform = BuildTransform(occurrence.Rotation, occurrence.Position),
            };

        private static double[] BuildTransform(double[] r, double[] p) => new[]
        {
            r[0], r[1], r[2], p[0],
            r[3], r[4], r[5], p[1],
            r[6], r[7], r[8], p[2],
            0.0,  0.0,  0.0,  1.0,
        };
    }
}
