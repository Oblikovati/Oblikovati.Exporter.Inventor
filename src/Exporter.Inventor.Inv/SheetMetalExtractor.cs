// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Inv
{
    /// <summary>
    /// Extracts a sheet-metal part as its FLAT PATTERN extruded by the sheet thickness. A
    /// sheet-metal solid's history is faces/flanges/bends, none of which the feature extractors
    /// read, so such parts otherwise export empty. Material is conserved through bends, so the
    /// unfolded flat pattern (its top face's outline and holes) extruded by the thickness has the
    /// same VOLUME as the folded part — the shape is unfolded, but the mass is faithful. The top
    /// face's edges are tessellated into a polyline sketch; the reader forms the plate region
    /// (outer boundary minus holes) and extrudes it.
    /// </summary>
    public static class SheetMetalExtractor
    {
        private const double StrokeToleranceCm = 0.01; // chord tolerance when sampling a curved edge

        public static void Extract(SheetMetalComponentDefinition sheet, InventorDocument ir)
        {
            if (!sheet.HasFlatPattern)
            {
                try { sheet.Unfold(); }
                catch (System.Runtime.InteropServices.COMException) { return; } // can't unfold -> skip
            }

            double thickness = sheet.Thickness._Value;
            Face top = sheet.FlatPattern.TopFace;
            if (thickness <= 0 || !(top.Geometry is Plane plane))
            {
                return;
            }

            double[] origin = P3(plane.RootPoint);
            (double[] xAxis, double[] yAxis) = FeatureExtractor.AxesFromNormal(plane.Normal);

            var sketch = new InventorSketch
            {
                Name = "FlatPattern",
                Origin = origin,
                XAxis = xAxis,
                YAxis = yAxis,
            };

            long nextId = 1;
            Edges edges = top.Edges;
            for (int i = 1; i <= edges.Count; i++)
            {
                List<double[]> poly = EdgePolyline(edges[i], origin, xAxis, yAxis);
                for (int k = 0; k + 1 < poly.Count; k++)
                {
                    sketch.Curves.Add(new InventorCurve
                    {
                        Id = nextId++,
                        Kind = InventorCurveKind.Line,
                        Start = poly[k],
                        End = poly[k + 1],
                    });
                }
            }

            if (sketch.Curves.Count == 0)
            {
                return;
            }

            SketchExtractor.InferCoincidences(sketch); // join the coincident segment endpoints
            ir.Sketches.Add(sketch);

            var extrude = new InventorExtrude
            {
                Name = "FlatPattern",
                SketchIndex = ir.Sketches.Count - 1,
                ProfileIndex = 0,
                Operation = InventorOperation.NewBody,
                ExtentKind = InventorExtentKind.Distance,
                Direction = InventorExtentDirection.Positive,
                Distance = thickness,
            };
            double[]? seed = FaceCentroid2d(top, origin, xAxis, yAxis);
            if (seed != null)
            {
                extrude.ProfileSeeds.Add(seed); // an interior point of the plate (top-face centroid)
            }

            ir.Features.Add(extrude);
        }

        // A B-rep edge as a 2D polyline in the flat-pattern plane. A curved edge is sampled with the
        // curve evaluator; a straight edge (or an evaluator that rejects sampling) uses its vertices.
        private static List<double[]> EdgePolyline(Edge edge, double[] origin, double[] x, double[] y)
        {
            var poly = new List<double[]>();
            try
            {
                CurveEvaluator evaluator = edge.Evaluator;
                evaluator.GetParamExtents(out double min, out double max);
                evaluator.GetStrokes(min, max, StrokeToleranceCm, out int count, out double[] coords);
                for (int i = 0; i < count; i++)
                {
                    poly.Add(Project(new[] { coords[i * 3], coords[i * 3 + 1], coords[i * 3 + 2] }, origin, x, y));
                }
            }
            catch (System.Exception)
            {
                poly.Clear();
            }

            if (poly.Count < 2)
            {
                Vertex start = edge.StartVertex;
                Vertex stop = edge.StopVertex;
                if (start != null && stop != null)
                {
                    poly.Clear();
                    poly.Add(Project(P3(start.Point), origin, x, y));
                    poly.Add(Project(P3(stop.Point), origin, x, y));
                }
            }

            return poly;
        }

        // Projects a model-space point onto the flat-pattern plane's 2D frame (cm).
        private static double[] Project(double[] p, double[] o, double[] x, double[] y)
        {
            double dx = p[0] - o[0], dy = p[1] - o[1], dz = p[2] - o[2];
            return new[] { dx * x[0] + dy * x[1] + dz * x[2], dx * y[0] + dy * y[1] + dz * y[2] };
        }

        // The 2D centroid of the top face's vertices — a point inside the plate, used as the
        // extrude's region seed (so the reader selects the plate, not a stray region).
        private static double[]? FaceCentroid2d(Face face, double[] o, double[] x, double[] y)
        {
            Vertices vertices = face.Vertices;
            int n = vertices.Count;
            if (n == 0)
            {
                return null;
            }

            double sx = 0, sy = 0, sz = 0;
            for (int i = 1; i <= n; i++)
            {
                Point p = vertices[i].Point;
                sx += p.X;
                sy += p.Y;
                sz += p.Z;
            }

            return Project(new[] { sx / n, sy / n, sz / n }, o, x, y);
        }

        private static double[] P3(Point p) => new[] { p.X, p.Y, p.Z };
    }
}
