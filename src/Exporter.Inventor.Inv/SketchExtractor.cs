// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Inventor;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Inv
{
    /// <summary>
    /// Reads a part's planar sketches into the IR. The plane frame comes straight from Inventor
    /// (origin = sketch origin, X axis = the sketch axis line, Y = plane-normal × X), and 2D
    /// point geometry is read in centimetres. Lines and circles are extracted; coincidence is
    /// inferred from endpoints that meet (so profiles close), and Inventor's explicit dimensions
    /// (distance/radius/diameter, with their parameter expressions) and orientation constraints
    /// (horizontal/vertical/parallel/perpendicular) are read too. Sketch entities are mapped to
    /// IR curves/points by COM identity (the runtime caches one RCW per COM object).
    /// </summary>
    public static class SketchExtractor
    {
        private const double CoincidenceTol = 1e-5; // cm, in sketch 2D

        public static void Extract(PartDocument document, InventorDocument ir, ExportReport report)
        {
            // The user parameters (already extracted into the IR) are the only names a dimension
            // expression may safely reference; a reference to any other (Inventor auto-named model)
            // parameter cannot round-trip and is collapsed to a value — see InventorExpression.ForDimension.
            var userParams = new HashSet<string>();
            foreach (InventorParameter p in ir.Parameters)
            {
                userParams.Add(p.Name);
            }

            PlanarSketches sketches = document.ComponentDefinition.Sketches;
            for (int i = 1; i <= sketches.Count; i++)
            {
                InventorSketch? extracted = ExtractOne(sketches[i], report, userParams);
                if (extracted != null)
                {
                    ir.Sketches.Add(extracted);
                }
            }
        }

        private static InventorSketch? ExtractOne(
            PlanarSketch sketch, ExportReport report, ISet<string> userParams)
        {
            UnitVector xAxis = sketch.AxisEntityGeometry.Direction;
            UnitVector yAxis = sketch.PlanarEntityGeometry.Normal.CrossProduct(xAxis);
            var result = new InventorSketch
            {
                Name = sketch.Name,
                Origin = P3(sketch.OriginPointGeometry),
                XAxis = V(xAxis),
                YAxis = V(yAxis),
            };

            // Maps from the Inventor sketch entities to the IR curves/points they became, so a
            // constraint/dimension referencing an entity resolves to the right curve id / point.
            var curveIds = new Dictionary<object, long>(RefComparer.Instance);
            var pointRefs = new Dictionary<object, InventorPointRef>(RefComparer.Instance);

            long nextId = 1;
            ExtractLines(sketch.SketchLines, result, curveIds, pointRefs, ref nextId);
            ExtractCircles(sketch.SketchCircles, result, curveIds, pointRefs, ref nextId);
            ExtractArcs(sketch.SketchArcs, result, curveIds, pointRefs, ref nextId);
            ExtractSplines(sketch.SketchSplines, result, curveIds, pointRefs, ref nextId);
            ExtractControlPointSplines(sketch.SketchControlPointSplines, result, curveIds, pointRefs, ref nextId);
            ExtractEllipses(sketch.SketchEllipses, result, curveIds, pointRefs, ref nextId);
            ExtractEllipticalArcs(sketch.SketchEllipticalArcs, result, curveIds, pointRefs, ref nextId);

            InferCoincidences(result);
            ExtractConstraints(sketch.GeometricConstraints, result, curveIds, pointRefs, report);
            ExtractDimensions(sketch.DimensionConstraints, result, curveIds, pointRefs, report, userParams);
            return result.Curves.Count == 0 ? null : result;
        }

        private static void ExtractLines(
            SketchLines lines, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs, ref long nextId)
        {
            for (int i = 1; i <= lines.Count; i++)
            {
                SketchLine line = lines[i];
                // A projected reference line can carry null Start/End sketch points; it is not part
                // of the sketch's own profile, so skip it rather than dereferencing null.
                SketchPoint start = line.StartSketchPoint;
                SketchPoint end = line.EndSketchPoint;
                if (start == null || end == null)
                {
                    continue;
                }

                long id = nextId++;
                result.Curves.Add(new InventorCurve
                {
                    Id = id,
                    Kind = InventorCurveKind.Line,
                    Start = P2(start.Geometry),
                    End = P2(end.Geometry),
                    Construction = line.Construction,
                });
                curveIds[line] = id;
                pointRefs[start] = new InventorPointRef(id, InventorCurvePointRole.Start);
                pointRefs[end] = new InventorPointRef(id, InventorCurvePointRole.End);
            }
        }

        private static void ExtractCircles(
            SketchCircles circles, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs, ref long nextId)
        {
            for (int i = 1; i <= circles.Count; i++)
            {
                SketchCircle circle = circles[i];
                long id = nextId++;
                result.Curves.Add(new InventorCurve
                {
                    Id = id,
                    Kind = InventorCurveKind.Circle,
                    Center = P2(circle.CenterSketchPoint.Geometry),
                    Radius = circle.Radius,
                    Construction = circle.Construction,
                });
                curveIds[circle] = id;
                pointRefs[circle.CenterSketchPoint] = new InventorPointRef(id, InventorCurvePointRole.Center);
            }
        }

        private static void ExtractArcs(
            SketchArcs arcs, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs, ref long nextId)
        {
            for (int i = 1; i <= arcs.Count; i++)
            {
                SketchArc arc = arcs[i];
                long id = nextId++;
                result.Curves.Add(new InventorCurve
                {
                    Id = id,
                    Kind = InventorCurveKind.Arc,
                    Center = P2(arc.CenterSketchPoint.Geometry),
                    Start = P2(arc.StartSketchPoint.Geometry),
                    End = P2(arc.EndSketchPoint.Geometry),
                    Ccw = arc.SweepAngle > 0, // positive sweep = counter-clockwise start->end
                    Construction = arc.Construction,
                });
                curveIds[arc] = id;
                pointRefs[arc.CenterSketchPoint] = new InventorPointRef(id, InventorCurvePointRole.Center);
                pointRefs[arc.StartSketchPoint] = new InventorPointRef(id, InventorCurvePointRole.Start);
                pointRefs[arc.EndSketchPoint] = new InventorPointRef(id, InventorCurvePointRole.End);
            }
        }

        private static void ExtractSplines(
            SketchSplines splines, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs, ref long nextId)
        {
            for (int i = 1; i <= splines.Count; i++)
            {
                SketchSpline spline = splines[i];
                long id = nextId++;
                var curve = new InventorCurve
                {
                    Id = id,
                    Kind = InventorCurveKind.Spline,
                    Closed = spline.Closed,
                    Fit = true, // a SketchSpline interpolates its fit points
                };
                for (int j = 1; j <= spline.FitPointCount; j++)
                {
                    SketchPoint fit = spline.get_FitPoint(j);
                    curve.SplinePoints.Add(P2(fit.Geometry));
                    pointRefs[fit] = new InventorPointRef(id, InventorCurvePointRole.SplinePoint, j - 1);
                }

                result.Curves.Add(curve);
                curveIds[spline] = id;
            }
        }

        private static void ExtractControlPointSplines(
            SketchControlPointSplines splines, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs, ref long nextId)
        {
            for (int i = 1; i <= splines.Count; i++)
            {
                SketchControlPointSpline spline = splines[i];
                long id = nextId++;
                var curve = new InventorCurve
                {
                    Id = id,
                    Kind = InventorCurveKind.Spline,
                    Closed = spline.IsClosed,
                    Fit = false, // a control-point spline is a control polygon, not interpolating
                };
                for (int j = 1; j <= spline.ControlPointCount; j++)
                {
                    SketchPoint control = spline.get_ControlPoint(j);
                    curve.SplinePoints.Add(P2(control.Geometry));
                    pointRefs[control] = new InventorPointRef(id, InventorCurvePointRole.SplinePoint, j - 1);
                }

                result.Curves.Add(curve);
                curveIds[spline] = id;
            }
        }

        private static void ExtractEllipses(
            SketchEllipses ellipses, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs, ref long nextId)
        {
            for (int i = 1; i <= ellipses.Count; i++)
            {
                SketchEllipse ellipse = ellipses[i];
                long id = nextId++;
                UnitVector2d major = ellipse.MajorAxisVector;
                result.Curves.Add(new InventorCurve
                {
                    Id = id,
                    Kind = InventorCurveKind.Ellipse,
                    Center = P2(ellipse.CenterSketchPoint.Geometry),
                    MajorAxis = new[] { major.X, major.Y },
                    MajorRadius = ellipse.MajorRadius,
                    MinorRadius = ellipse.MinorRadius,
                    Construction = ellipse.Construction,
                });
                curveIds[ellipse] = id;
                pointRefs[ellipse.CenterSketchPoint] = new InventorPointRef(id, InventorCurvePointRole.Center);
            }
        }

        private static void ExtractEllipticalArcs(
            SketchEllipticalArcs arcs, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs, ref long nextId)
        {
            for (int i = 1; i <= arcs.Count; i++)
            {
                SketchEllipticalArc arc = arcs[i];
                long id = nextId++;
                UnitVector2d major = arc.MajorAxisVector;
                result.Curves.Add(new InventorCurve
                {
                    Id = id,
                    Kind = InventorCurveKind.EllipticalArc,
                    Center = P2(arc.CenterSketchPoint.Geometry),
                    MajorAxis = new[] { major.X, major.Y },
                    MajorRadius = arc.MajorRadius,
                    MinorRadius = arc.MinorRadius,
                    StartAngle = arc.StartAngle,
                    EndAngle = arc.StartAngle + arc.SweepAngle, // Inventor gives start + included sweep
                    Construction = arc.Construction,
                });
                curveIds[arc] = id;
                pointRefs[arc.CenterSketchPoint] = new InventorPointRef(id, InventorCurvePointRole.Center);
            }
        }

        // Reads the orientation/relation constraints (coincidence is already inferred). A handled
        // constraint whose operand did not map to extracted geometry (e.g. an unsupported entity)
        // is recorded on the report rather than dropped silently; constraint types not handled here
        // (coincidence and any we don't model) fall through untouched and are not reported.
        private static void ExtractConstraints(
            GeometricConstraints constraints, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs, ExportReport report)
        {
            for (int i = 1; i <= constraints.Count; i++)
            {
                bool ok;
                string kind;
                switch (constraints[i])
                {
                    case HorizontalConstraint h:
                        kind = "horizontal";
                        ok = AddOnCurve(result, InventorConstraintKind.Horizontal, curveIds, h.Entity);
                        break;
                    case VerticalConstraint v:
                        kind = "vertical";
                        ok = AddOnCurve(result, InventorConstraintKind.Vertical, curveIds, v.Entity);
                        break;
                    case ParallelConstraint p:
                        kind = "parallel";
                        ok = AddBetweenCurves(result, InventorConstraintKind.Parallel, curveIds, p.EntityOne, p.EntityTwo);
                        break;
                    case PerpendicularConstraint pp:
                        kind = "perpendicular";
                        ok = AddBetweenCurves(result, InventorConstraintKind.Perpendicular, curveIds, pp.EntityOne, pp.EntityTwo);
                        break;
                    case CollinearConstraint col:
                        kind = "collinear";
                        ok = AddBetweenCurves(result, InventorConstraintKind.Collinear, curveIds, col.EntityOne, col.EntityTwo);
                        break;
                    case ConcentricConstraint con:
                        kind = "concentric";
                        ok = AddBetweenCurves(result, InventorConstraintKind.Concentric, curveIds, con.EntityOne, con.EntityTwo);
                        break;
                    case TangentConstraint tan:
                        kind = "tangent";
                        ok = AddBetweenCurves(result, InventorConstraintKind.Tangent, curveIds, tan.EntityOne, tan.EntityTwo);
                        break;
                    case EqualLengthConstraint eq:
                        kind = "equal-length";
                        ok = AddBetweenCurves(result, InventorConstraintKind.EqualLength, curveIds, eq.LineOne, eq.LineTwo);
                        break;
                    case EqualRadiusConstraint er:
                        kind = "equal-radius";
                        ok = AddBetweenCurves(result, InventorConstraintKind.EqualRadius, curveIds, er.EntityOne, er.EntityTwo);
                        break;
                    case SymmetryConstraint sym:
                        kind = "symmetry";
                        ok = AddSymmetry(result, curveIds, pointRefs, sym);
                        break;
                    case GroundConstraint g:
                        kind = "ground";
                        ok = AddGround(result, curveIds, pointRefs, g.Entity);
                        break;
                    case SmoothConstraint sm:
                        kind = "smooth";
                        ok = AddSmooth(result, curveIds, sm.EntityOne, sm.EntityTwo);
                        break;
                    default:
                        continue; // not modelled here (e.g. coincidence, inferred geometrically)
                }

                if (!ok)
                {
                    report.Skip(
                        $"sketch-constraint '{kind}' in sketch '{result.Name}'",
                        "could not be represented from the extracted sketch geometry");
                }
            }
        }

        private static void ExtractDimensions(
            DimensionConstraints dimensions, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs,
            ExportReport report, ISet<string> userParams)
        {
            for (int i = 1; i <= dimensions.Count; i++)
            {
                bool ok;
                string kind;
                switch (dimensions[i])
                {
                    case TwoPointDistanceDimConstraint d:
                        kind = "distance";
                        ok = AddDistance(result, pointRefs, d, userParams);
                        break;
                    case RadiusDimConstraint r:
                        kind = "radius";
                        ok = AddCurveDimension(result, InventorDimensionKind.Radius, curveIds, r.Entity, r.Parameter, userParams);
                        break;
                    case DiameterDimConstraint dia:
                        kind = "diameter";
                        ok = AddCurveDimension(result, InventorDimensionKind.Diameter, curveIds, dia.Entity, dia.Parameter, userParams);
                        break;
                    case TwoLineAngleDimConstraint ang:
                        kind = "angle";
                        ok = AddAngleDimension(result, curveIds, ang, userParams);
                        break;
                    default:
                        continue; // dimension type we don't model
                }

                if (!ok)
                {
                    report.Skip(
                        $"sketch-dimension '{kind}' in sketch '{result.Name}'",
                        "could not be represented from the extracted sketch geometry");
                }
            }
        }

        // Each Add* returns true when it emitted the constraint/dimension, false when an operand
        // could not be resolved to extracted geometry (so the caller can record the drop).
        private static bool AddOnCurve(
            InventorSketch result, InventorConstraintKind kind, IDictionary<object, long> curveIds, object entity)
        {
            if (curveIds.TryGetValue(entity, out long id))
            {
                var c = new InventorSketchConstraint { Kind = kind };
                c.Curves.Add(id);
                result.Constraints.Add(c);
                return true;
            }

            return false;
        }

        private static bool AddBetweenCurves(
            InventorSketch result, InventorConstraintKind kind, IDictionary<object, long> curveIds, object a, object b)
        {
            if (curveIds.TryGetValue(a, out long ida) && curveIds.TryGetValue(b, out long idb))
            {
                var c = new InventorSketchConstraint { Kind = kind };
                c.Curves.Add(ida);
                c.Curves.Add(idb);
                result.Constraints.Add(c);
                return true;
            }

            return false;
        }

        // Two entities symmetric about a line. The engine's symmetry is point-based, so this is
        // read only when both entities resolve to points (e.g. curve endpoints) and the axis to a curve.
        private static bool AddSymmetry(
            InventorSketch result, IDictionary<object, long> curveIds,
            IDictionary<object, InventorPointRef> pointRefs, SymmetryConstraint sym)
        {
            if (pointRefs.TryGetValue(sym.EntityOne, out InventorPointRef a) &&
                pointRefs.TryGetValue(sym.EntityTwo, out InventorPointRef b) &&
                curveIds.TryGetValue(sym.SymmetryLine, out long axis))
            {
                var c = new InventorSketchConstraint { Kind = InventorConstraintKind.Symmetry };
                c.Points.Add(a);
                c.Points.Add(b);
                c.Curves.Add(axis);
                result.Constraints.Add(c);
                return true;
            }

            return false;
        }

        // Grounds (fixes) an entity by pinning its defining points. Inventor can ground either a
        // curve (fixing all of its points) or a single sketch point (e.g. pinning an origin corner),
        // so resolve a curve via curveIds and a point via pointRefs.
        private static bool AddGround(
            InventorSketch result, IDictionary<object, long> curveIds,
            IDictionary<object, InventorPointRef> pointRefs, object entity)
        {
            var c = new InventorSketchConstraint { Kind = InventorConstraintKind.Ground };

            if (curveIds.TryGetValue(entity, out long id))
            {
                InventorCurve? curve = FindCurve(result, id);
                if (curve == null)
                {
                    return false;
                }

                foreach (InventorPointRef p in PointRefsOf(curve))
                {
                    c.Points.Add(p);
                }
            }
            else if (pointRefs.TryGetValue(entity, out InventorPointRef point))
            {
                c.Points.Add(point);
            }

            if (c.Points.Count > 0)
            {
                result.Constraints.Add(c);
                return true;
            }

            return false;
        }

        // Smooth (G2) between two curves: emit the two curves plus their coincident junction
        // points (the shared endpoint, one ref per curve), which the engine's smooth needs.
        private static bool AddSmooth(
            InventorSketch result, IDictionary<object, long> curveIds, object entityOne, object entityTwo)
        {
            if (!curveIds.TryGetValue(entityOne, out long id1) || !curveIds.TryGetValue(entityTwo, out long id2))
            {
                return false;
            }

            InventorCurve? a = FindCurve(result, id1);
            InventorCurve? b = FindCurve(result, id2);
            if (a == null || b == null)
            {
                return false;
            }

            foreach ((InventorPointRef Ref, double[] Pt) ea in EndpointSlots(a))
            {
                foreach ((InventorPointRef Ref, double[] Pt) eb in EndpointSlots(b))
                {
                    if (Distance2D(ea.Pt, eb.Pt) <= CoincidenceTol)
                    {
                        var c = new InventorSketchConstraint { Kind = InventorConstraintKind.Smooth };
                        c.Points.Add(ea.Ref);
                        c.Points.Add(eb.Ref);
                        c.Curves.Add(id1);
                        c.Curves.Add(id2);
                        result.Constraints.Add(c);
                        return true;
                    }
                }
            }

            return false;
        }

        // A curve's free endpoints (where it can join another smoothly) with their coordinates.
        private static IEnumerable<(InventorPointRef Ref, double[] Pt)> EndpointSlots(InventorCurve c)
        {
            switch (c.Kind)
            {
                case InventorCurveKind.Line:
                case InventorCurveKind.Arc:
                    yield return (new InventorPointRef(c.Id, InventorCurvePointRole.Start), c.Start);
                    yield return (new InventorPointRef(c.Id, InventorCurvePointRole.End), c.End);
                    break;
                case InventorCurveKind.Spline when c.SplinePoints.Count > 0:
                    yield return (new InventorPointRef(c.Id, InventorCurvePointRole.SplinePoint, 0), c.SplinePoints[0]);
                    int last = c.SplinePoints.Count - 1;
                    yield return (new InventorPointRef(c.Id, InventorCurvePointRole.SplinePoint, last), c.SplinePoints[last]);
                    break;
            }
        }

        private static InventorCurve? FindCurve(InventorSketch sketch, long id)
        {
            foreach (InventorCurve c in sketch.Curves)
            {
                if (c.Id == id)
                {
                    return c;
                }
            }

            return null;
        }

        // The defining points of a curve, by role (matching how the point table allocates them).
        private static IEnumerable<InventorPointRef> PointRefsOf(InventorCurve curve)
        {
            switch (curve.Kind)
            {
                case InventorCurveKind.Line:
                    yield return new InventorPointRef(curve.Id, InventorCurvePointRole.Start);
                    yield return new InventorPointRef(curve.Id, InventorCurvePointRole.End);
                    break;
                case InventorCurveKind.Arc:
                    yield return new InventorPointRef(curve.Id, InventorCurvePointRole.Center);
                    yield return new InventorPointRef(curve.Id, InventorCurvePointRole.Start);
                    yield return new InventorPointRef(curve.Id, InventorCurvePointRole.End);
                    break;
                case InventorCurveKind.Spline:
                    for (int i = 0; i < curve.SplinePoints.Count; i++)
                    {
                        yield return new InventorPointRef(curve.Id, InventorCurvePointRole.SplinePoint, i);
                    }

                    break;
                default: // Circle, Ellipse, EllipticalArc
                    yield return new InventorPointRef(curve.Id, InventorCurvePointRole.Center);
                    break;
            }
        }

        private static bool AddDistance(
            InventorSketch result, IDictionary<object, InventorPointRef> pointRefs,
            TwoPointDistanceDimConstraint d, ISet<string> userParams)
        {
            if (pointRefs.TryGetValue(d.PointOne, out InventorPointRef a) &&
                pointRefs.TryGetValue(d.PointTwo, out InventorPointRef b))
            {
                var dim = new InventorSketchDimension
                {
                    Kind = InventorDimensionKind.Distance,
                    Expression = DimensionExpression(d.Parameter, isAngle: false, userParams),
                };
                dim.Points.Add(a);
                dim.Points.Add(b);
                result.Dimensions.Add(dim);
                return true;
            }

            return false;
        }

        private static bool AddCurveDimension(
            InventorSketch result, InventorDimensionKind kind,
            IDictionary<object, long> curveIds, object entity, Parameter parameter, ISet<string> userParams)
        {
            if (curveIds.TryGetValue(entity, out long id))
            {
                var dim = new InventorSketchDimension
                {
                    Kind = kind,
                    Expression = DimensionExpression(parameter, isAngle: false, userParams),
                };
                dim.Curves.Add(id);
                result.Dimensions.Add(dim);
                return true;
            }

            return false;
        }

        private static bool AddAngleDimension(
            InventorSketch result, IDictionary<object, long> curveIds,
            TwoLineAngleDimConstraint a, ISet<string> userParams)
        {
            if (curveIds.TryGetValue(a.LineOne, out long ida) && curveIds.TryGetValue(a.LineTwo, out long idb))
            {
                var dim = new InventorSketchDimension
                {
                    Kind = InventorDimensionKind.Angle,
                    Expression = DimensionExpression(a.Parameter, isAngle: true, userParams),
                };
                dim.Curves.Add(ida);
                dim.Curves.Add(idb);
                result.Dimensions.Add(dim);
                return true;
            }

            return false;
        }

        // A dimension's driving expression, made safe for the reader: verbatim when it is a literal
        // or references only user parameters, else collapsed to its model value (see
        // InventorExpression.ForDimension). _Value is the evaluated value in database units.
        private static string DimensionExpression(Parameter parameter, bool isAngle, ISet<string> userParams) =>
            InventorExpression.ForDimension(parameter.Expression, parameter._Value, isAngle, userParams);

        // Reference-equality dictionary so the same COM entity (one RCW per COM object) resolves
        // to the curve/point it became, independent of value equality.
        private sealed class RefComparer : IEqualityComparer<object>
        {
            public static readonly RefComparer Instance = new RefComparer();

            bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        // Emit a coincident constraint for each pair of line endpoints that meet, so the profile
        // closes (mirrors how the engine records coincidence between distinct points).
        private static void InferCoincidences(InventorSketch sketch)
        {
            var slots = new List<(InventorPointRef Ref, double[] Pt)>();
            foreach (InventorCurve c in sketch.Curves)
            {
                // Lines and arcs have start/end endpoints that join a profile; circles do not.
                if (c.Kind != InventorCurveKind.Line && c.Kind != InventorCurveKind.Arc)
                {
                    continue;
                }

                slots.Add((new InventorPointRef(c.Id, InventorCurvePointRole.Start), c.Start));
                slots.Add((new InventorPointRef(c.Id, InventorCurvePointRole.End), c.End));
            }

            for (int i = 0; i < slots.Count; i++)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (slots[i].Ref.CurveId == slots[j].Ref.CurveId)
                    {
                        continue;
                    }

                    if (Distance2D(slots[i].Pt, slots[j].Pt) <= CoincidenceTol)
                    {
                        var con = new InventorSketchConstraint { Kind = InventorConstraintKind.Coincident };
                        con.Points.Add(slots[i].Ref);
                        con.Points.Add(slots[j].Ref);
                        sketch.Constraints.Add(con);
                    }
                }
            }
        }

        private static double[] P3(Point p) => new[] { p.X, p.Y, p.Z };

        private static double[] P2(Point2d p) => new[] { p.X, p.Y };

        private static double[] V(UnitVector v) => new[] { v.X, v.Y, v.Z };

        private static double Distance2D(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1];
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
