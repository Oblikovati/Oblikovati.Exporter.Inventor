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
    /// inferred from endpoints that meet, so profiles close in Oblikovati. Inventor's explicit
    /// geometric constraints and dimensions are the parametric refinement and are read in a
    /// follow-up; the geometry here is positioned correctly.
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

            long nextId = 1;
            ExtractLines(sketch.SketchLines, result, ref nextId);
            ExtractCircles(sketch.SketchCircles, result, ref nextId);

            InferCoincidences(result);
            return result.Curves.Count == 0 ? null : result;
        }

        private static void ExtractLines(SketchLines lines, InventorSketch result, ref long nextId)
        {
            for (int i = 1; i <= lines.Count; i++)
            {
                SketchLine line = lines[i];
                result.Curves.Add(new InventorCurve
                {
                    Id = nextId++,
                    Kind = InventorCurveKind.Line,
                    Start = P2(line.StartSketchPoint.Geometry),
                    End = P2(line.EndSketchPoint.Geometry),
                    Construction = line.Construction,
                });
            }
        }

        private static void ExtractCircles(SketchCircles circles, InventorSketch result, ref long nextId)
        {
            for (int i = 1; i <= circles.Count; i++)
            {
                SketchCircle circle = circles[i];
                result.Curves.Add(new InventorCurve
                {
                    Id = nextId++,
                    Kind = InventorCurveKind.Circle,
                    Center = P2(circle.CenterSketchPoint.Geometry),
                    Radius = circle.Radius,
                    Construction = circle.Construction,
                });
            }
        }

        // Emit a coincident constraint for each pair of line endpoints that meet, so the profile
        // closes (mirrors how the engine records coincidence between distinct points).
        private static void InferCoincidences(InventorSketch sketch)
        {
            var slots = new List<(InventorPointRef Ref, double[] Pt)>();
            foreach (InventorCurve c in sketch.Curves)
            {
                if (c.Kind != InventorCurveKind.Line)
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
