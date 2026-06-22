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

    /// <summary>
    /// A revolve of a sketch profile about the sketch's own centerline (the profile sketch must
    /// contain a line marked <see cref="InventorCurve.Centerline"/>). <see cref="AngleRadians"/>
    /// of 0 means a full revolution.
    /// </summary>
    public sealed class InventorRevolve : InventorFeature
    {
        public int SketchIndex { get; set; }

        public int ProfileIndex { get; set; }

        public InventorOperation Operation { get; set; } = InventorOperation.NewBody;

        /// <summary>Swept angle in radians; 0 means a full revolution.</summary>
        public double AngleRadians { get; set; }
    }

    /// <summary>
    /// Base of features that replicate earlier features. <see cref="SourceFeatureIndices"/> are
    /// indices into <see cref="InventorDocument.Features"/> (resolved to program indices on
    /// translation); they must refer to earlier, translatable features.
    /// </summary>
    public abstract class InventorReplicatingFeature : InventorFeature
    {
        public System.Collections.Generic.IList<int> SourceFeatureIndices { get; } =
            new System.Collections.Generic.List<int>();
    }

    /// <summary>A rectangular grid pattern. Step vectors are the offset between adjacent copies (cm).</summary>
    public sealed class InventorRectangularPattern : InventorReplicatingFeature
    {
        public int CountX { get; set; } = 1;

        public int CountY { get; set; } = 1;

        public double[] StepX { get; set; } = { 0, 0, 0 };

        public double[] StepY { get; set; } = { 0, 0, 0 };
    }

    /// <summary>A circular pattern about an axis. AngleRadians is the total spread (0 = full 360).</summary>
    public sealed class InventorCircularPattern : InventorReplicatingFeature
    {
        public int Count { get; set; } = 1;

        public double AngleRadians { get; set; }

        public double[] AxisPoint { get; set; } = { 0, 0, 0 };

        public double[] AxisDir { get; set; } = { 0, 0, 1 };
    }

    /// <summary>A mirror across a plane given by its origin (cm) and unit normal.</summary>
    public sealed class InventorMirror : InventorReplicatingFeature
    {
        public double[] PlaneOrigin { get; set; } = { 0, 0, 0 };

        public double[] PlaneNormal { get; set; } = { 1, 0, 0 };
    }
}
