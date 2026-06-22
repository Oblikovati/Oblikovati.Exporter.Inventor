// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor 2D-sketch read surface the adapter walks. Shapes mirror
// the genuine Autodesk.Inventor.Interop (verified by compiling the adapter against the real
// assembly): geometry collections are 1-based and indexed by int, PlanarSketches by object;
// a sketch's model-space frame comes from OriginPointGeometry / AxisEntityGeometry / the
// plane normal, and 2D point geometry is read in centimetres (Inventor's database unit).
namespace Inventor
{
    /// <summary>Stub of the planar-sketches collection (object-indexed, 1-based).</summary>
    public class PlanarSketches
    {
        public virtual int Count => throw Stub.Error();

        public virtual PlanarSketch this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one planar sketch: a plane frame plus geometry collections.</summary>
    public class PlanarSketch
    {
        public virtual string Name => throw Stub.Error();

        public virtual SketchLines SketchLines => throw Stub.Error();

        public virtual SketchCircles SketchCircles => throw Stub.Error();

        /// <summary>The sketch origin in model space (maps to sketch 2D (0,0)).</summary>
        public virtual Point OriginPointGeometry => throw Stub.Error();

        /// <summary>The sketch X axis as a model-space line.</summary>
        public virtual Line AxisEntityGeometry => throw Stub.Error();

        /// <summary>The plane the sketch lives on (its normal completes the frame).</summary>
        public virtual Plane PlanarEntityGeometry => throw Stub.Error();
    }

    /// <summary>Stub of the sketch-lines collection (int-indexed, 1-based).</summary>
    public class SketchLines
    {
        public virtual int Count => throw Stub.Error();

        public virtual SketchLine this[int index] => throw Stub.Error();
    }

    /// <summary>Stub of one sketch line.</summary>
    public class SketchLine
    {
        public virtual SketchPoint StartSketchPoint => throw Stub.Error();

        public virtual SketchPoint EndSketchPoint => throw Stub.Error();

        public virtual bool Construction => throw Stub.Error();
    }

    /// <summary>Stub of the sketch-circles collection (int-indexed, 1-based).</summary>
    public class SketchCircles
    {
        public virtual int Count => throw Stub.Error();

        public virtual SketchCircle this[int index] => throw Stub.Error();
    }

    /// <summary>Stub of one sketch circle.</summary>
    public class SketchCircle
    {
        public virtual SketchPoint CenterSketchPoint => throw Stub.Error();

        public virtual double Radius => throw Stub.Error();

        public virtual bool Construction => throw Stub.Error();
    }

    /// <summary>Stub of one sketch point; its 2D geometry is in sketch space (cm).</summary>
    public class SketchPoint
    {
        public virtual Point2d Geometry => throw Stub.Error();
    }

    /// <summary>Stub of a transient 2D point (cm).</summary>
    public class Point2d
    {
        public virtual double X => throw Stub.Error();

        public virtual double Y => throw Stub.Error();
    }

    /// <summary>Stub of a transient 3D point (cm).</summary>
    public class Point
    {
        public virtual double X => throw Stub.Error();

        public virtual double Y => throw Stub.Error();

        public virtual double Z => throw Stub.Error();
    }

    /// <summary>Stub of a transient unit vector, with the cross product used to complete a frame.</summary>
    public class UnitVector
    {
        public virtual double X => throw Stub.Error();

        public virtual double Y => throw Stub.Error();

        public virtual double Z => throw Stub.Error();

        public virtual UnitVector CrossProduct(UnitVector other) => throw Stub.Error();
    }

    /// <summary>Stub of a transient plane (its root point + normal place a work plane).</summary>
    public class Plane
    {
        public virtual Point RootPoint => throw Stub.Error();

        public virtual UnitVector Normal => throw Stub.Error();
    }

    /// <summary>Stub of a transient line (its direction is the sketch X axis).</summary>
    public class Line
    {
        public virtual UnitVector Direction => throw Stub.Error();
    }
}
