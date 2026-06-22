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
        Spline,
        Ellipse,
        EllipticalArc,
    }

    /// <summary>
    /// Which defining point of a curve a constraint/dimension refers to. <see cref="SplinePoint"/>
    /// is indexed (a spline has an ordered list of fit points); the others ignore the index.
    /// </summary>
    public enum InventorCurvePointRole
    {
        Start,
        End,
        Center,
        SplinePoint,
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

        /// <summary>A spline's ordered fit points (2D, cm). Used only when Kind is Spline.</summary>
        public IList<double[]> SplinePoints { get; } = new List<double[]>();

        /// <summary>Whether a spline is closed (forms its own loop).</summary>
        public bool Closed { get; set; }

        /// <summary>Whether a spline interpolates its points (fit) rather than being a control polygon.</summary>
        public bool Fit { get; set; }

        /// <summary>Ellipse only: the major-axis direction (2D unit vector).</summary>
        public double[] MajorAxis { get; set; } = { 1, 0 };

        /// <summary>Ellipse only: the major semi-axis length (cm).</summary>
        public double MajorRadius { get; set; }

        /// <summary>Ellipse only: the minor semi-axis length (cm).</summary>
        public double MinorRadius { get; set; }

        /// <summary>Elliptical arc only: the start angle (radians, about the major axis).</summary>
        public double StartAngle { get; set; }

        /// <summary>Elliptical arc only: the end angle (radians).</summary>
        public double EndAngle { get; set; }
    }

    /// <summary>
    /// A reference to one defining point of a curve (e.g. a line's end point, or a spline's
    /// indexed fit point).
    /// </summary>
    public readonly struct InventorPointRef
    {
        public InventorPointRef(long curveId, InventorCurvePointRole role, int index = 0)
        {
            CurveId = curveId;
            Role = role;
            Index = index;
        }

        public long CurveId { get; }

        public InventorCurvePointRole Role { get; }

        /// <summary>The fit-point index for a <see cref="InventorCurvePointRole.SplinePoint"/>; else 0.</summary>
        public int Index { get; }
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
        Symmetry,
        Ground,
        Smooth,
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
