// SPDX-License-Identifier: GPL-2.0-only
namespace Oblikovati.Exporter.Inventor.Model
{
    /// <summary>Boolean operation a feature performs against existing bodies.</summary>
    public enum InventorOperation
    {
        NewBody,
        Join,
        Cut,
        Intersect,
    }

    /// <summary>Which way a single-distance extent grows from its sketch plane.</summary>
    public enum InventorExtentDirection
    {
        Positive,
        Negative,
        Symmetric,
    }

    /// <summary>Base of an extracted Inventor feature. The translator dispatches on the concrete type.</summary>
    public abstract class InventorFeature
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// An extrude of a sketch profile. <see cref="SketchIndex"/> is the index into
    /// <see cref="InventorDocument.Sketches"/>; <see cref="ProfileIndex"/> selects a detected
    /// region of that sketch. Lengths are centimetres (Inventor's database unit = the recipe
    /// unit); the depth is an evaluated value (the recipe extent is not parameter-driven yet).
    /// </summary>
    public sealed class InventorExtrude : InventorFeature
    {
        public int SketchIndex { get; set; }

        public int ProfileIndex { get; set; }

        public InventorOperation Operation { get; set; } = InventorOperation.NewBody;

        public InventorExtentDirection Direction { get; set; } = InventorExtentDirection.Positive;

        public double Distance { get; set; }

        /// <summary>Second-direction distance for an asymmetric two-sided extrude (cm).</summary>
        public double SecondDistance { get; set; }

        /// <summary>Draft/taper angle in radians (0 for a straight extrude).</summary>
        public double TaperRadians { get; set; }
    }
}
