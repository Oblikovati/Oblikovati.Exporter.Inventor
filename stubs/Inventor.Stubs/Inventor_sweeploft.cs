// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor loft read surface. A loft's Sections is a heterogeneous
// ObjectCollection whose profile items are cast to Profile and resolved to their parent sketch.
namespace Inventor
{
    /// <summary>Stub of the loft-features collection (object-indexed, 1-based).</summary>
    public class LoftFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual LoftFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one loft feature: its ordered profile sections and the boolean operation.</summary>
    public class LoftFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual PartFeatureOperationEnum Operation => throw Stub.Error();

        public virtual ObjectCollection Sections => throw Stub.Error();
    }

    /// <summary>Stub of the sweep-features collection (object-indexed, 1-based).</summary>
    public class SweepFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual SweepFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one sweep feature: a profile swept along a path.</summary>
    public class SweepFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual PartFeatureOperationEnum Operation => throw Stub.Error();

        public virtual Profile Profile => throw Stub.Error();

        public virtual Path Path => throw Stub.Error();
    }

    /// <summary>Stub of a sweep path: a 1-based sequence of path entities.</summary>
    public class Path
    {
        public virtual int Count => throw Stub.Error();

        public virtual PathEntity this[int index] => throw Stub.Error();
    }

    /// <summary>
    /// Stub of one path entity. SketchEntity is the underlying sketch curve (cast to SketchLine
    /// for a straight segment); OpposedToSketchEntity flips the segment's sense along the path.
    /// </summary>
    public class PathEntity
    {
        public virtual object SketchEntity => throw Stub.Error();

        public virtual bool OpposedToSketchEntity => throw Stub.Error();
    }

    /// <summary>Stub of a transient 3D line segment (a sketch line's model-space geometry).</summary>
    public class LineSegment
    {
        public virtual Point StartPoint => throw Stub.Error();

        public virtual Point EndPoint => throw Stub.Error();
    }
}
