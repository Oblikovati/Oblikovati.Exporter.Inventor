// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.Inventor.Model
{
    /// <summary>
    /// An Inventor sketch in Inventor-neutral terms: a plane plus curves, geometric
    /// constraints and dimensions that reference those curves by a local id. The translator
    /// turns this into Oblikovati's sketch model. All lengths are in CENTIMETRES — Inventor's
    /// internal database unit, which is also Oblikovati's recipe unit, so no scaling is needed.
    /// </summary>
    public sealed class InventorSketch
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Plane origin in model space (cm).</summary>
        public double[] Origin { get; set; } = { 0, 0, 0 };

        /// <summary>In-plane X axis (unit vector, model space).</summary>
        public double[] XAxis { get; set; } = { 1, 0, 0 };

        /// <summary>In-plane Y axis (unit vector, model space).</summary>
        public double[] YAxis { get; set; } = { 0, 1, 0 };

        public IList<InventorCurve> Curves { get; } = new List<InventorCurve>();

        public IList<InventorSketchConstraint> Constraints { get; } = new List<InventorSketchConstraint>();

        public IList<InventorSketchDimension> Dimensions { get; } = new List<InventorSketchDimension>();
    }

    public enum InventorCurveKind
    {
        Line,
        Circle,
        Arc,
    }

    /// <summary>Which defining point of a curve a constraint/dimension refers to.</summary>
    public enum InventorCurvePointRole
    {
        Start,
        End,
        Center,
    }

    /// <summary>
    /// One sketch curve. Coordinates are 2D in sketch space (cm). A line uses Start/End; a
    /// circle uses Center/Radius; an arc uses Center/Start/End plus Ccw.
    /// </summary>
    public sealed class InventorCurve
    {
        public long Id { get; set; }

        public InventorCurveKind Kind { get; set; }

        public double[] Start { get; set; } = { 0, 0 };

        public double[] End { get; set; } = { 0, 0 };

        public double[] Center { get; set; } = { 0, 0 };

        public double Radius { get; set; }

        public bool Ccw { get; set; }

        public bool Construction { get; set; }

        /// <summary>A line that acts as an axis (excluded from profiles; used as a revolve axis).</summary>
        public bool Centerline { get; set; }
    }

    /// <summary>A reference to one defining point of a curve (e.g. a line's end point).</summary>
    public readonly struct InventorPointRef
    {
        public InventorPointRef(long curveId, InventorCurvePointRole role)
        {
            CurveId = curveId;
            Role = role;
        }

        public long CurveId { get; }

        public InventorCurvePointRole Role { get; }
    }

    public enum InventorConstraintKind
    {
        Coincident,
        Horizontal,
        Vertical,
        Parallel,
        Perpendicular,
        Collinear,
        EqualLength,
        Concentric,
        EqualRadius,
        Tangent,
        PointOnLine,
        Midpoint,
        Fix,
    }

    /// <summary>
    /// One geometric constraint. <see cref="Points"/> carries point-ref operands;
    /// <see cref="Curves"/> carries curve-id operands. The translator knows which to use
    /// per kind (e.g. parallel uses two curves; coincident uses two points).
    /// </summary>
    public sealed class InventorSketchConstraint
    {
        public InventorConstraintKind Kind { get; set; }

        public IList<InventorPointRef> Points { get; } = new List<InventorPointRef>();

        public IList<long> Curves { get; } = new List<long>();
    }

    public enum InventorDimensionKind
    {
        Distance,
        Radius,
        Diameter,
        Angle,
    }

    /// <summary>
    /// One dimensional constraint. <see cref="Expression"/> is the parameter expression
    /// driving it (e.g. "width" or "40 mm"); a driven (reference) dimension measures rather
    /// than drives.
    /// </summary>
    public sealed class InventorSketchDimension
    {
        public InventorDimensionKind Kind { get; set; }

        public IList<InventorPointRef> Points { get; } = new List<InventorPointRef>();

        public IList<long> Curves { get; } = new List<long>();

        public string Expression { get; set; } = string.Empty;

        public bool Driven { get; set; }
    }
}
