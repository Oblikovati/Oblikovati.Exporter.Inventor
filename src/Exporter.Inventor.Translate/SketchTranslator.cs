// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>
    /// Translates one Inventor sketch into an Oblikovati <see cref="SketchData"/>: distinct
    /// points joined by coincident constraints (via <see cref="SketchPointTable"/>), curve
    /// entities, geometric constraints, and parameter-linked dimensions. Coordinates pass
    /// through unchanged — Inventor's database unit (cm) is the recipe unit.
    /// </summary>
    public sealed class SketchTranslator
    {
        private readonly IdAllocator _ids;
        private readonly ExportReport _report;

        public SketchTranslator(IdAllocator ids, ExportReport report)
        {
            _ids = ids ?? throw new ArgumentNullException(nameof(ids));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public SketchData Translate(InventorSketch sketch, int sketchId)
        {
            var points = new SketchPointTable();
            points.Build(sketch, _ids);

            var data = new SketchData
            {
                Id = sketchId,
                Name = sketch.Name.Length == 0 ? null : sketch.Name,
                Plane = TranslatePlane(sketch),
            };
            foreach (PointData p in points.Points)
            {
                data.Points.Add(p);
            }

            Dictionary<long, int> entityIds = AddEntities(sketch, points, data);
            AddConstraints(sketch, points, entityIds, data);
            AddDimensions(sketch, points, entityIds, data);
            return data;
        }

        private static PlaneData TranslatePlane(InventorSketch sketch) => new PlaneData
        {
            Origin = (double[])sketch.Origin.Clone(),
            XAxis = (double[])sketch.XAxis.Clone(),
            YAxis = (double[])sketch.YAxis.Clone(),
        };

        // Allocates an entity id per curve (after points) and emits its EntityData.
        private Dictionary<long, int> AddEntities(InventorSketch sketch, SketchPointTable points, SketchData data)
        {
            var entityIds = new Dictionary<long, int>();
            foreach (InventorCurve curve in sketch.Curves)
            {
                int id = _ids.Next();
                entityIds[curve.Id] = id;
                data.Entities.Add(BuildEntity(id, curve, points));
            }

            return entityIds;
        }

        private static EntityData BuildEntity(int id, InventorCurve curve, SketchPointTable points)
        {
            var entity = new EntityData
            {
                Id = id,
                Kind = KindName(curve.Kind),
                Construction = curve.Construction ? true : (bool?)null,
            };
            switch (curve.Kind)
            {
                case InventorCurveKind.Line:
                    entity.Points.Add(points.PointId(new InventorPointRef(curve.Id, InventorCurvePointRole.Start)));
                    entity.Points.Add(points.PointId(new InventorPointRef(curve.Id, InventorCurvePointRole.End)));
                    entity.Centerline = curve.Centerline ? true : (bool?)null;
                    break;
                case InventorCurveKind.Circle:
                    entity.Points.Add(points.PointId(new InventorPointRef(curve.Id, InventorCurvePointRole.Center)));
                    entity.Radius = curve.Radius;
                    break;
                case InventorCurveKind.Arc:
                    entity.Points.Add(points.PointId(new InventorPointRef(curve.Id, InventorCurvePointRole.Center)));
                    entity.Points.Add(points.PointId(new InventorPointRef(curve.Id, InventorCurvePointRole.Start)));
                    entity.Points.Add(points.PointId(new InventorPointRef(curve.Id, InventorCurvePointRole.End)));
                    entity.Ccw = curve.Ccw ? true : (bool?)null;
                    break;
                default: // Spline
                    foreach (int pid in points.SplinePointIds(curve.Id))
                    {
                        entity.Points.Add(pid);
                    }

                    entity.Closed = curve.Closed ? true : (bool?)null;
                    entity.Fit = curve.Fit ? true : (bool?)null;
                    break;
            }

            return entity;
        }

        private void AddConstraints(
            InventorSketch sketch, SketchPointTable points, Dictionary<long, int> entityIds, SketchData data)
        {
            foreach (InventorSketchConstraint c in sketch.Constraints)
            {
                ConstraintData? row = BuildConstraint(c, points, entityIds);
                if (row != null)
                {
                    data.Constraints.Add(row);
                }
            }
        }

        private ConstraintData? BuildConstraint(
            InventorSketchConstraint c, SketchPointTable points, Dictionary<long, int> entityIds)
        {
            var row = new ConstraintData { Kind = ConstraintName(c.Kind) };
            switch (c.Kind)
            {
                case InventorConstraintKind.Coincident:
                    // Distinct endpoints joined by an explicit coincidence (engine format).
                    row.Points.Add(points.PointId(c.Points[0]));
                    row.Points.Add(points.PointId(c.Points[1]));
                    return row;
                case InventorConstraintKind.Horizontal:
                case InventorConstraintKind.Vertical:
                    // Inventor applies these to a line; Oblikovati constrains the line's endpoints.
                    long line = c.Curves[0];
                    row.Points.Add(points.PointId(new InventorPointRef(line, InventorCurvePointRole.Start)));
                    row.Points.Add(points.PointId(new InventorPointRef(line, InventorCurvePointRole.End)));
                    return row;
                case InventorConstraintKind.Parallel:
                case InventorConstraintKind.Perpendicular:
                case InventorConstraintKind.Collinear:
                case InventorConstraintKind.EqualLength:
                case InventorConstraintKind.Concentric:
                case InventorConstraintKind.EqualRadius:
                case InventorConstraintKind.Tangent:
                    foreach (long id in c.Curves)
                    {
                        row.Curves.Add(entityIds[id]);
                    }

                    return row;
                case InventorConstraintKind.PointOnLine:
                case InventorConstraintKind.Midpoint:
                    row.Points.Add(points.PointId(c.Points[0]));
                    row.Curves.Add(entityIds[c.Curves[0]]);
                    return row;
                case InventorConstraintKind.Fix:
                    row.Points.Add(points.PointId(c.Points[0]));
                    return row;
                default:
                    _report.Skip("sketch-constraint", c.Kind.ToString());
                    return null;
            }
        }

        private void AddDimensions(
            InventorSketch sketch, SketchPointTable points, Dictionary<long, int> entityIds, SketchData data)
        {
            foreach (InventorSketchDimension d in sketch.Dimensions)
            {
                data.Dimensions.Add(BuildDimension(d, points, entityIds));
            }
        }

        private static DimensionData BuildDimension(
            InventorSketchDimension d, SketchPointTable points, Dictionary<long, int> entityIds)
        {
            var row = new DimensionData
            {
                Kind = DimensionName(d.Kind),
                Expression = d.Expression,
                Driven = d.Driven ? true : (bool?)null,
            };
            if (d.Kind == InventorDimensionKind.Distance)
            {
                foreach (InventorPointRef p in d.Points)
                {
                    row.Points.Add(points.PointId(p));
                }
            }
            else
            {
                foreach (long id in d.Curves)
                {
                    row.Curves.Add(entityIds[id]);
                }
            }

            return row;
        }

        private static string KindName(InventorCurveKind kind) => kind switch
        {
            InventorCurveKind.Line => "line",
            InventorCurveKind.Circle => "circle",
            InventorCurveKind.Arc => "arc",
            _ => "spline",
        };

        private static string ConstraintName(InventorConstraintKind kind) => kind switch
        {
            InventorConstraintKind.Coincident => "coincident",
            InventorConstraintKind.Horizontal => "horizontal",
            InventorConstraintKind.Vertical => "vertical",
            InventorConstraintKind.Parallel => "parallel",
            InventorConstraintKind.Perpendicular => "perpendicular",
            InventorConstraintKind.Collinear => "collinear",
            InventorConstraintKind.EqualLength => "equalLength",
            InventorConstraintKind.Concentric => "concentric",
            InventorConstraintKind.EqualRadius => "equalRadius",
            InventorConstraintKind.Tangent => "tangent",
            InventorConstraintKind.PointOnLine => "pointOnLine",
            InventorConstraintKind.Midpoint => "midpoint",
            InventorConstraintKind.Fix => "fix",
            _ => kind.ToString(),
        };

        private static string DimensionName(InventorDimensionKind kind) => kind switch
        {
            InventorDimensionKind.Distance => "distance",
            InventorDimensionKind.Radius => "radius",
            InventorDimensionKind.Diameter => "diameter",
            _ => "angle",
        };
    }
}
