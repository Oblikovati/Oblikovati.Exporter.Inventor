// SPDX-License-Identifier: GPL-2.0-only
using System;
using Inventor;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Inv
{
    /// <summary>
    /// Reads a part's work planes and history features into the IR. Work planes become
    /// fixed-frame datums (the three default origin planes are skipped); extrudes with a
    /// distance extent are read with their operation, direction and depth (cm). Other extent
    /// types and feature kinds are the next milestones — they are left out rather than
    /// guessed, so the rest of the history still exports.
    /// </summary>
    public static class FeatureExtractor
    {
        private static readonly string[] OriginPlaneNames = { "XY Plane", "XZ Plane", "YZ Plane" };

        public static void Extract(PartDocument document, InventorDocument ir)
        {
            PartComponentDefinition definition = document.ComponentDefinition;
            ExtractWorkPlanes(definition.WorkPlanes, ir);
            ExtractExtrudes(definition.Features.ExtrudeFeatures, ir);
        }

        private static void ExtractWorkPlanes(WorkPlanes planes, InventorDocument ir)
        {
            for (int i = 1; i <= planes.Count; i++)
            {
                WorkPlane wp = planes[i];
                if (IsOriginPlane(wp.Name))
                {
                    continue;
                }

                Plane plane = wp.Plane;
                double[] origin = P3(plane.RootPoint);
                (double[] xAxis, double[] yAxis) = AxesFromNormal(plane.Normal);
                ir.WorkPlanes.Add(new InventorWorkPlane
                {
                    Name = wp.Name,
                    Origin = origin,
                    XAxis = xAxis,
                    YAxis = yAxis,
                });
            }
        }

        private static void ExtractExtrudes(ExtrudeFeatures extrudes, InventorDocument ir)
        {
            for (int i = 1; i <= extrudes.Count; i++)
            {
                ExtrudeFeature ext = extrudes[i];
                if (!(ext.Definition.Extent is DistanceExtent distance))
                {
                    continue; // Only distance extents are read for now.
                }

                int sketchIndex = SketchIndexOf(ir, ((PlanarSketch)ext.Profile.Parent).Name);
                if (sketchIndex < 0)
                {
                    continue;
                }

                ir.Features.Add(new InventorExtrude
                {
                    Name = ext.Name,
                    SketchIndex = sketchIndex,
                    ProfileIndex = 0,
                    Operation = ToOperation(ext.Operation),
                    Direction = ToDirection(distance.Direction),
                    Distance = distance.Distance._Value,
                });
            }
        }

        private static int SketchIndexOf(InventorDocument ir, string sketchName)
        {
            for (int i = 0; i < ir.Sketches.Count; i++)
            {
                if (ir.Sketches[i].Name == sketchName)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsOriginPlane(string name) => Array.IndexOf(OriginPlaneNames, name) >= 0;

        // A datum carries no preferred in-plane axes, so pick an arbitrary orthonormal pair:
        // X perpendicular to the normal, Y = normal × X.
        private static (double[] X, double[] Y) AxesFromNormal(UnitVector normal)
        {
            double[] n = V(normal);
            double[] seed = Math.Abs(n[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double[] x = Normalize(Cross(n, seed));
            double[] y = Normalize(Cross(n, x));
            return (x, y);
        }

        private static InventorOperation ToOperation(PartFeatureOperationEnum op) => op switch
        {
            PartFeatureOperationEnum.kJoinOperation => InventorOperation.Join,
            PartFeatureOperationEnum.kCutOperation => InventorOperation.Cut,
            PartFeatureOperationEnum.kIntersectOperation => InventorOperation.Intersect,
            _ => InventorOperation.NewBody,
        };

        private static InventorExtentDirection ToDirection(PartFeatureExtentDirectionEnum dir) => dir switch
        {
            PartFeatureExtentDirectionEnum.kNegativeExtentDirection => InventorExtentDirection.Negative,
            PartFeatureExtentDirectionEnum.kSymmetricExtentDirection => InventorExtentDirection.Symmetric,
            _ => InventorExtentDirection.Positive,
        };

        private static double[] P3(Point p) => new[] { p.X, p.Y, p.Z };

        private static double[] V(UnitVector v) => new[] { v.X, v.Y, v.Z };

        private static double[] Cross(double[] a, double[] b) => new[]
        {
            a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0],
        };

        private static double[] Normalize(double[] a)
        {
            double len = Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]);
            return len == 0 ? new double[] { 0, 0, 0 } : new[] { a[0] / len, a[1] / len, a[2] / len };
        }
    }
}
