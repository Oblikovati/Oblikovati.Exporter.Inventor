// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>
    /// Maps an Inventor work plane to a fixed-frame work plane: its origin and two in-plane
    /// unit axes. A fixed frame carries the datum's solved geometry faithfully without
    /// re-deriving the Inventor construction. Coordinates pass through (cm).
    /// </summary>
    public static class WorkPlaneTranslator
    {
        public static WorkFeatureData Translate(InventorWorkPlane plane) => new WorkFeatureData
        {
            Collection = "plane",
            Kind = "fixed-frame",
            Position = (double[])plane.Origin.Clone(),
            XAxis = (double[])plane.XAxis.Clone(),
            YAxis = (double[])plane.YAxis.Clone(),
        };
    }
}
