// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor feature + work-plane read surface the adapter walks.
// Shapes mirror the genuine Autodesk.Inventor.Interop (verified by compiling the adapter
// against the real assembly): collections are 1-based and object-indexed, and the extrude
// extent distance is read from a DistanceExtent's Parameter (database units = cm).
namespace Inventor
{
    /// <summary>Boolean operation a feature performs.</summary>
    public enum PartFeatureOperationEnum
    {
        kJoinOperation = 20481,
        kCutOperation = 20482,
        kIntersectOperation = 20483,
        kNewBodyOperation = 20485,
    }

    /// <summary>How a feature's extent is defined.</summary>
    public enum PartFeatureExtentEnum
    {
        kDistanceExtent = 20737,
        kAngleExtent = 20738,
        kFullSweepExtent = 20739,
        kThroughAllExtent = 20743,
    }

    /// <summary>Which way a single-distance extent grows from the sketch plane.</summary>
    public enum PartFeatureExtentDirectionEnum
    {
        kPositiveExtentDirection = 20993,
        kNegativeExtentDirection = 20994,
        kSymmetricExtentDirection = 20995,
    }

    /// <summary>Stub of the feature collections root.</summary>
    public class PartFeatures
    {
        public virtual ExtrudeFeatures ExtrudeFeatures => throw Stub.Error();

        public virtual RevolveFeatures RevolveFeatures => throw Stub.Error();

        public virtual RectangularPatternFeatures RectangularPatternFeatures => throw Stub.Error();

        public virtual CircularPatternFeatures CircularPatternFeatures => throw Stub.Error();

        public virtual MirrorFeatures MirrorFeatures => throw Stub.Error();

        public virtual FilletFeatures FilletFeatures => throw Stub.Error();

        public virtual ChamferFeatures ChamferFeatures => throw Stub.Error();

        public virtual ShellFeatures ShellFeatures => throw Stub.Error();

        public virtual FaceDraftFeatures FaceDraftFeatures => throw Stub.Error();

        public virtual HoleFeatures HoleFeatures => throw Stub.Error();
    }

    /// <summary>Common base of the modelling features, exposing the Name a pattern reads.</summary>
    public class PartFeature
    {
        public virtual string Name => throw Stub.Error();
    }

    /// <summary>Stub of a heterogeneous object collection (e.g. a pattern's parent features).</summary>
    public class ObjectCollection
    {
        public virtual int Count => throw Stub.Error();

        public virtual object this[int index] => throw Stub.Error();
    }

    /// <summary>Stub of the rectangular-pattern features collection (object-indexed, 1-based).</summary>
    public class RectangularPatternFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual RectangularPatternFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one rectangular pattern. Direction entities are typed object (work axis / edge).</summary>
    public class RectangularPatternFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual ObjectCollection ParentFeatures => throw Stub.Error();

        public virtual Parameter XCount => throw Stub.Error();

        public virtual Parameter YCount => throw Stub.Error();

        public virtual Parameter XSpacing => throw Stub.Error();

        public virtual Parameter YSpacing => throw Stub.Error();

        public virtual object XDirectionEntity => throw Stub.Error();

        public virtual object YDirectionEntity => throw Stub.Error();

        public virtual bool NaturalXDirection => throw Stub.Error();

        public virtual bool NaturalYDirection => throw Stub.Error();
    }

    /// <summary>Stub of the circular-pattern features collection (object-indexed, 1-based).</summary>
    public class CircularPatternFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual CircularPatternFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one circular pattern. The axis entity is typed object (work axis / edge).</summary>
    public class CircularPatternFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual ObjectCollection ParentFeatures => throw Stub.Error();

        public virtual Parameter Count => throw Stub.Error();

        public virtual Parameter Angle => throw Stub.Error();

        public virtual object AxisEntity => throw Stub.Error();

        public virtual bool NaturalAxisDirection => throw Stub.Error();
    }

    /// <summary>Stub of the mirror features collection (object-indexed, 1-based).</summary>
    public class MirrorFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual MirrorFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one mirror. The plane entity is typed object (work plane / planar face).</summary>
    public class MirrorFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual ObjectCollection ParentFeatures => throw Stub.Error();

        public virtual object MirrorPlaneEntity => throw Stub.Error();
    }

    /// <summary>Stub of a work axis (its Line gives a point + direction).</summary>
    public class WorkAxis
    {
        public virtual Line Line => throw Stub.Error();
    }

    /// <summary>Stub of a B-rep edge; its vertices give a straight edge's endpoints.</summary>
    public class Edge
    {
        public virtual Vertex StartVertex => throw Stub.Error();

        public virtual Vertex StopVertex => throw Stub.Error();
    }

    /// <summary>Stub of a B-rep vertex.</summary>
    public class Vertex
    {
        public virtual Point Point => throw Stub.Error();
    }

    /// <summary>Stub of a B-rep face; Geometry is the underlying surface (a Plane for a planar face).</summary>
    public class Face
    {
        public virtual object Geometry => throw Stub.Error();

        public virtual Vertices Vertices => throw Stub.Error();
    }

    /// <summary>Stub of the revolve-features collection (object-indexed, 1-based).</summary>
    public class RevolveFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual RevolveFeature this[object index] => throw Stub.Error();
    }

    /// <summary>
    /// Stub of one revolve feature. The axis is the strongly-typed <see cref="_AxisEntity"/>
    /// sketch line; the angle (for a non-full revolve) comes from an AngleExtent.
    /// </summary>
    public class RevolveFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual PartFeatureOperationEnum Operation => throw Stub.Error();

        public virtual Profile Profile => throw Stub.Error();

        public virtual SketchLine _AxisEntity => throw Stub.Error();

        public virtual PartFeatureExtentEnum ExtentType => throw Stub.Error();

        public virtual PartFeatureExtent Extent => throw Stub.Error();
    }

    /// <summary>Stub of an angle extent (revolve sweep angle, in radians).</summary>
    public class AngleExtent : PartFeatureExtent
    {
        public virtual Parameter Angle => throw Stub.Error();
    }

    /// <summary>Stub of the extrude-features collection (object-indexed, 1-based).</summary>
    public class ExtrudeFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual ExtrudeFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one extrude feature.</summary>
    public class ExtrudeFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual PartFeatureOperationEnum Operation => throw Stub.Error();

        public virtual Profile Profile => throw Stub.Error();

        public virtual ExtrudeDefinition Definition => throw Stub.Error();
    }

    /// <summary>Stub of an extrude definition (the extent carries the distance).</summary>
    public class ExtrudeDefinition
    {
        public virtual PartFeatureExtentEnum ExtentType => throw Stub.Error();

        public virtual PartFeatureExtent Extent => throw Stub.Error();
    }

    /// <summary>Base of the extent objects (the concrete kind is keyed by ExtentType).</summary>
    public class PartFeatureExtent
    {
    }

    /// <summary>Stub of a distance extent: a distance Parameter plus a direction.</summary>
    public class DistanceExtent : PartFeatureExtent
    {
        public virtual Parameter Distance => throw Stub.Error();

        public virtual PartFeatureExtentDirectionEnum Direction => throw Stub.Error();
    }

    /// <summary>
    /// Stub of a non-user parameter (e.g. a feature's distance). Distinct COM interface from
    /// UserParameter. <c>_Value</c> is the evaluated numeric value in database units (cm).
    /// </summary>
    public class Parameter
    {
        public virtual double _Value => throw Stub.Error();

        /// <summary>The editable expression (a literal like "40 mm" or a reference like "width").</summary>
        public virtual string Expression => throw Stub.Error();
    }

    /// <summary>Stub of one sketch profile; its Parent is the sketch it was built from.</summary>
    public class Profile
    {
        public virtual PlanarSketch Parent => throw Stub.Error();
    }

    /// <summary>Stub of the work-planes collection (object-indexed, 1-based).</summary>
    public class WorkPlanes
    {
        public virtual int Count => throw Stub.Error();

        public virtual WorkPlane this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one work plane; its geometry gives origin + normal.</summary>
    public class WorkPlane
    {
        public virtual string Name => throw Stub.Error();

        public virtual Plane Plane => throw Stub.Error();
    }
}
