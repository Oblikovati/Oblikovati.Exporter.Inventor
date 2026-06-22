// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor sketch geometric-constraint and dimension read surface.
// Shapes mirror the genuine Autodesk.Inventor.Interop; collections are 1-based and int-indexed,
// and a constraint's referenced entities (object-typed SketchEntity) are cast to SketchLine /
// SketchCircle and resolved by geometry in the adapter.
namespace Inventor
{
    /// <summary>Stub of a sketch's geometric-constraints collection.</summary>
    public class GeometricConstraints
    {
        public virtual int Count => throw Stub.Error();

        public virtual GeometricConstraint this[int index] => throw Stub.Error();
    }

    /// <summary>Base of the geometric constraints (the concrete COM type is matched at runtime).</summary>
    public class GeometricConstraint
    {
    }

    /// <summary>Stub of a horizontal constraint on a line (Entity is the SketchLine).</summary>
    public class HorizontalConstraint : GeometricConstraint
    {
        public virtual object Entity => throw Stub.Error();
    }

    /// <summary>Stub of a vertical constraint on a line.</summary>
    public class VerticalConstraint : GeometricConstraint
    {
        public virtual object Entity => throw Stub.Error();
    }

    /// <summary>Stub of a parallel constraint between two lines.</summary>
    public class ParallelConstraint : GeometricConstraint
    {
        public virtual object EntityOne => throw Stub.Error();

        public virtual object EntityTwo => throw Stub.Error();
    }

    /// <summary>Stub of a perpendicular constraint between two lines.</summary>
    public class PerpendicularConstraint : GeometricConstraint
    {
        public virtual object EntityOne => throw Stub.Error();

        public virtual object EntityTwo => throw Stub.Error();
    }

    /// <summary>Stub of a collinear constraint between two lines.</summary>
    public class CollinearConstraint : GeometricConstraint
    {
        public virtual object EntityOne => throw Stub.Error();

        public virtual object EntityTwo => throw Stub.Error();
    }

    /// <summary>Stub of a concentric constraint between two circles/arcs.</summary>
    public class ConcentricConstraint : GeometricConstraint
    {
        public virtual object EntityOne => throw Stub.Error();

        public virtual object EntityTwo => throw Stub.Error();
    }

    /// <summary>Stub of a tangent constraint between two curves.</summary>
    public class TangentConstraint : GeometricConstraint
    {
        public virtual object EntityOne => throw Stub.Error();

        public virtual object EntityTwo => throw Stub.Error();
    }

    /// <summary>Stub of an equal-length constraint between two lines.</summary>
    public class EqualLengthConstraint : GeometricConstraint
    {
        public virtual object LineOne => throw Stub.Error();

        public virtual object LineTwo => throw Stub.Error();
    }

    /// <summary>Stub of an equal-radius constraint between two circles/arcs.</summary>
    public class EqualRadiusConstraint : GeometricConstraint
    {
        public virtual object EntityOne => throw Stub.Error();

        public virtual object EntityTwo => throw Stub.Error();
    }

    /// <summary>Stub of a sketch's dimension-constraints collection.</summary>
    public class DimensionConstraints
    {
        public virtual int Count => throw Stub.Error();

        public virtual DimensionConstraint this[int index] => throw Stub.Error();
    }

    /// <summary>Base of the dimension constraints (the concrete COM type is matched at runtime).</summary>
    public class DimensionConstraint
    {
    }

    /// <summary>Stub of a two-point distance dimension; its Parameter carries the expression.</summary>
    public class TwoPointDistanceDimConstraint : DimensionConstraint
    {
        public virtual SketchPoint PointOne => throw Stub.Error();

        public virtual SketchPoint PointTwo => throw Stub.Error();

        public virtual Parameter Parameter => throw Stub.Error();
    }

    /// <summary>Stub of a radius dimension on a circle/arc (Entity); its Parameter carries the expression.</summary>
    public class RadiusDimConstraint : DimensionConstraint
    {
        public virtual object Entity => throw Stub.Error();

        public virtual Parameter Parameter => throw Stub.Error();
    }

    /// <summary>Stub of a diameter dimension on a circle (Entity); its Parameter carries the expression.</summary>
    public class DiameterDimConstraint : DimensionConstraint
    {
        public virtual object Entity => throw Stub.Error();

        public virtual Parameter Parameter => throw Stub.Error();
    }

    /// <summary>Stub of an angle dimension between two lines; its Parameter carries the expression.</summary>
    public class TwoLineAngleDimConstraint : DimensionConstraint
    {
        public virtual object LineOne => throw Stub.Error();

        public virtual object LineTwo => throw Stub.Error();

        public virtual Parameter Parameter => throw Stub.Error();
    }
}
