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

        public static void Extract(PartDocument document, InventorDocument ir)
        {
            PlanarSketches sketches = document.ComponentDefinition.Sketches;
            for (int i = 1; i <= sketches.Count; i++)
            {
                InventorSketch? extracted = ExtractOne(sketches[i]);
                if (extracted != null)
                {
                    ir.Sketches.Add(extracted);
                }
            }
        }

        private static InventorSketch? ExtractOne(PlanarSketch sketch)
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

            InferCoincidences(result);
            ExtractConstraints(sketch.GeometricConstraints, result, curveIds);
            ExtractDimensions(sketch.DimensionConstraints, result, curveIds, pointRefs);
            return result.Curves.Count == 0 ? null : result;
        }

        private static void ExtractLines(
            SketchLines lines, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs, ref long nextId)
        {
            for (int i = 1; i <= lines.Count; i++)
            {
                SketchLine line = lines[i];
                long id = nextId++;
                result.Curves.Add(new InventorCurve
                {
                    Id = id,
                    Kind = InventorCurveKind.Line,
                    Start = P2(line.StartSketchPoint.Geometry),
                    End = P2(line.EndSketchPoint.Geometry),
                    Construction = line.Construction,
                });
                curveIds[line] = id;
                pointRefs[line.StartSketchPoint] = new InventorPointRef(id, InventorCurvePointRole.Start);
                pointRefs[line.EndSketchPoint] = new InventorPointRef(id, InventorCurvePointRole.End);
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

        // Reads the orientation/relation constraints (coincidence is already inferred). An operand
        // that did not map to an extracted curve (e.g. an unsupported entity) skips the constraint.
        private static void ExtractConstraints(
            GeometricConstraints constraints, InventorSketch result, IDictionary<object, long> curveIds)
        {
            for (int i = 1; i <= constraints.Count; i++)
            {
                switch (constraints[i])
                {
                    case HorizontalConstraint h:
                        AddOnCurve(result, InventorConstraintKind.Horizontal, curveIds, h.Entity);
                        break;
                    case VerticalConstraint v:
                        AddOnCurve(result, InventorConstraintKind.Vertical, curveIds, v.Entity);
                        break;
                    case ParallelConstraint p:
                        AddBetweenCurves(result, InventorConstraintKind.Parallel, curveIds, p.EntityOne, p.EntityTwo);
                        break;
                    case PerpendicularConstraint pp:
                        AddBetweenCurves(result, InventorConstraintKind.Perpendicular, curveIds, pp.EntityOne, pp.EntityTwo);
                        break;
                }
            }
        }

        private static void ExtractDimensions(
            DimensionConstraints dimensions, InventorSketch result,
            IDictionary<object, long> curveIds, IDictionary<object, InventorPointRef> pointRefs)
        {
            for (int i = 1; i <= dimensions.Count; i++)
            {
                switch (dimensions[i])
                {
                    case TwoPointDistanceDimConstraint d:
                        AddDistance(result, pointRefs, d);
                        break;
                    case RadiusDimConstraint r:
                        AddCurveDimension(result, InventorDimensionKind.Radius, curveIds, r.Entity, r.Parameter);
                        break;
                    case DiameterDimConstraint dia:
                        AddCurveDimension(result, InventorDimensionKind.Diameter, curveIds, dia.Entity, dia.Parameter);
                        break;
                }
            }
        }

        private static void AddOnCurve(
            InventorSketch result, InventorConstraintKind kind, IDictionary<object, long> curveIds, object entity)
        {
            if (curveIds.TryGetValue(entity, out long id))
            {
                var c = new InventorSketchConstraint { Kind = kind };
                c.Curves.Add(id);
                result.Constraints.Add(c);
            }
        }

        private static void AddBetweenCurves(
            InventorSketch result, InventorConstraintKind kind, IDictionary<object, long> curveIds, object a, object b)
        {
            if (curveIds.TryGetValue(a, out long ida) && curveIds.TryGetValue(b, out long idb))
            {
                var c = new InventorSketchConstraint { Kind = kind };
                c.Curves.Add(ida);
                c.Curves.Add(idb);
                result.Constraints.Add(c);
            }
        }

        private static void AddDistance(
            InventorSketch result, IDictionary<object, InventorPointRef> pointRefs, TwoPointDistanceDimConstraint d)
        {
            if (pointRefs.TryGetValue(d.PointOne, out InventorPointRef a) &&
                pointRefs.TryGetValue(d.PointTwo, out InventorPointRef b))
            {
                var dim = new InventorSketchDimension
                {
                    Kind = InventorDimensionKind.Distance,
                    Expression = d.Parameter.Expression,
                };
                dim.Points.Add(a);
                dim.Points.Add(b);
                result.Dimensions.Add(dim);
            }
        }

        private static void AddCurveDimension(
            InventorSketch result, InventorDimensionKind kind,
            IDictionary<object, long> curveIds, object entity, Parameter parameter)
        {
            if (curveIds.TryGetValue(entity, out long id))
            {
                var dim = new InventorSketchDimension { Kind = kind, Expression = parameter.Expression };
                dim.Curves.Add(id);
                result.Dimensions.Add(dim);
            }
        }

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
