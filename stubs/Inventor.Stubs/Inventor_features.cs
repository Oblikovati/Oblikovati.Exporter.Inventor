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

    /// <summary>How a feature's extent is defined (only the distance extent is read for now).</summary>
    public enum PartFeatureExtentEnum
    {
        kDistanceExtent = 20737,
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
