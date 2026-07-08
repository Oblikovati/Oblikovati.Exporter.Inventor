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

    /// <summary>How an extrude terminates. Distance uses <see cref="InventorExtrude.Distance"/>;
    /// ThroughAll/ToNext span the existing material (the engine resolves the span).</summary>
    public enum InventorExtentKind
    {
        Distance,
        ThroughAll,
        ToNext,
        ToFace,
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

        /// <summary>How the extrude terminates (distance vs through-all / to-next).</summary>
        public InventorExtentKind ExtentKind { get; set; } = InventorExtentKind.Distance;

        public InventorExtentDirection Direction { get; set; } = InventorExtentDirection.Positive;

        /// <summary>Depth for a <see cref="InventorExtentKind.Distance"/> extent (cm); unused otherwise.</summary>
        public double Distance { get; set; }

        /// <summary>Second-direction distance for an asymmetric two-sided extrude (cm).</summary>
        public double SecondDistance { get; set; }

        /// <summary>Draft/taper angle in radians (0 for a straight extrude).</summary>
        public double TaperRadians { get; set; }

        /// <summary>For a <see cref="InventorExtentKind.ToFace"/> extent: a point on the planar
        /// termination face (model cm) and its normal. The reader names the stop face by this
        /// geometry (toFaceGeom) since an exporter cannot mint the host's face key. Null otherwise.</summary>
        public double[]? ToFaceCentroid { get; set; }

        /// <summary>Normal of the <see cref="InventorExtentKind.ToFace"/> termination face (model unit).</summary>
        public double[]? ToFaceNormal { get; set; }

        /// <summary>One interior seed point (sketch 2D, cm) per selected profile region. The reader
        /// resolves each to the region that contains it, replacing the fragile <see cref="ProfileIndex"/>
        /// (the reader's region ordering is not predictable). Empty ⇒ fall back to the index.</summary>
        public System.Collections.Generic.IList<double[]> ProfileSeeds { get; } =
            new System.Collections.Generic.List<double[]>();

        /// <summary>The feature's selected profile as Inventor resolved it (the loops of
        /// Profile.ProfilePaths). When non-empty the emitter authors these loops into a dedicated
        /// sketch and extrudes region 0 — faithful, and immune to the shared-sketch region ambiguity
        /// that <see cref="ProfileSeeds"/> is subject to. Empty ⇒ fall back to sketch + seeds.</summary>
        public System.Collections.Generic.IList<InventorProfileLoop> ProfileLoops { get; } =
            new System.Collections.Generic.List<InventorProfileLoop>();
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

        /// <summary>The line index (among the profile sketch's line-kind curves) of this revolve's
        /// own injected axis centerline, or -1 for own-centerline mode. Emitting it disambiguates
        /// the axis when several revolves share one sketch (each injects a centerline, so the
        /// reader's "single centerline" fallback would be ambiguous).</summary>
        public int AxisLineIndex { get; set; } = -1;

        /// <summary>Interior seed point(s) (sketch 2D, cm) selecting the revolved region by
        /// containment rather than the fragile <see cref="ProfileIndex"/>. The translator emits the
        /// first; empty ⇒ fall back to the index.</summary>
        public System.Collections.Generic.IList<double[]> ProfileSeeds { get; } =
            new System.Collections.Generic.List<double[]>();

        /// <summary>The revolve's selected profile as Inventor resolved it (Profile.ProfilePaths).
        /// When non-empty the emitter authors these loops into a dedicated sketch and revolves about
        /// the axis resolved from the original sketch's centerline. Empty ⇒ fall back to sketch +
        /// seeds. See <see cref="InventorExtrude.ProfileLoops"/>.</summary>
        public System.Collections.Generic.IList<InventorProfileLoop> ProfileLoops { get; } =
            new System.Collections.Generic.List<InventorProfileLoop>();
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
