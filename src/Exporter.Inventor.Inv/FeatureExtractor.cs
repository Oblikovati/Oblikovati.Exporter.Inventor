// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
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
            PartFeatures features = definition.Features;
            ExtractWorkPlanes(definition.WorkPlanes, ir);
            ExtractExtrudes(features.ExtrudeFeatures, ir);
            ExtractRevolves(features.RevolveFeatures, ir);
            // Patterns/mirror reference earlier features by name, so extract them last.
            ExtractRectangularPatterns(features.RectangularPatternFeatures, ir);
            ExtractCircularPatterns(features.CircularPatternFeatures, ir);
            ExtractMirrors(features.MirrorFeatures, ir);
        }

        private static void ExtractRectangularPatterns(RectangularPatternFeatures patterns, InventorDocument ir)
        {
            for (int i = 1; i <= patterns.Count; i++)
            {
                RectangularPatternFeature p = patterns[i];
                double[]? xDir = ResolveDirection(p.XDirectionEntity, p.NaturalXDirection);
                if (xDir == null || !TryResolveSources(p.ParentFeatures, ir, out var sources))
                {
                    continue; // unresolved direction or source -> skip rather than guess
                }

                var pattern = new InventorRectangularPattern
                {
                    Name = p.Name,
                    CountX = (int)p.XCount._Value,
                    CountY = (int)p.YCount._Value,
                    StepX = Scale(xDir, p.XSpacing._Value),
                };
                double[]? yDir = ResolveDirection(p.YDirectionEntity, p.NaturalYDirection);
                if (pattern.CountY > 1 && yDir != null)
                {
                    pattern.StepY = Scale(yDir, p.YSpacing._Value);
                }

                AddSources(pattern, sources);
                ir.Features.Add(pattern);
            }
        }

        private static void ExtractCircularPatterns(CircularPatternFeatures patterns, InventorDocument ir)
        {
            for (int i = 1; i <= patterns.Count; i++)
            {
                CircularPatternFeature p = patterns[i];
                (double[] point, double[] dir)? axis = ResolveAxis(p.AxisEntity, p.NaturalAxisDirection);
                if (axis == null || !TryResolveSources(p.ParentFeatures, ir, out var sources))
                {
                    continue;
                }

                var pattern = new InventorCircularPattern
                {
                    Name = p.Name,
                    Count = (int)p.Count._Value,
                    AngleRadians = p.Angle._Value,
                    AxisPoint = axis.Value.point,
                    AxisDir = axis.Value.dir,
                };
                AddSources(pattern, sources);
                ir.Features.Add(pattern);
            }
        }

        private static void ExtractMirrors(MirrorFeatures mirrors, InventorDocument ir)
        {
            for (int i = 1; i <= mirrors.Count; i++)
            {
                MirrorFeature m = mirrors[i];
                (double[] origin, double[] normal)? plane = ResolvePlane(m.MirrorPlaneEntity);
                if (plane == null || !TryResolveSources(m.ParentFeatures, ir, out var sources))
                {
                    continue;
                }

                var mirror = new InventorMirror
                {
                    Name = m.Name,
                    PlaneOrigin = plane.Value.origin,
                    PlaneNormal = plane.Value.normal,
                };
                AddSources(mirror, sources);
                ir.Features.Add(mirror);
            }
        }

        // Maps a pattern's parent features (by name) to IR feature indices; fails if any is unknown.
        private static bool TryResolveSources(ObjectCollection parents, InventorDocument ir, out List<int> sources)
        {
            sources = new List<int>();
            for (int i = 1; i <= parents.Count; i++)
            {
                int idx = FeatureIndexOf(ir, ((PartFeature)parents[i]).Name);
                if (idx < 0)
                {
                    return false;
                }

                sources.Add(idx);
            }

            return sources.Count > 0;
        }

        private static int FeatureIndexOf(InventorDocument ir, string name)
        {
            for (int i = 0; i < ir.Features.Count; i++)
            {
                if (ir.Features[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        // A unit direction from a work axis or a straight edge (negated when not the natural sense).
        private static double[]? ResolveDirection(object entity, bool natural)
        {
            double[]? dir = entity switch
            {
                WorkAxis axis => V(axis.Line.Direction),
                Edge edge => Normalize(Sub(P3(edge.StopVertex.Point), P3(edge.StartVertex.Point))),
                _ => null,
            };
            if (dir == null || (dir[0] == 0 && dir[1] == 0 && dir[2] == 0))
            {
                return null;
            }

            return natural ? dir : new[] { -dir[0], -dir[1], -dir[2] };
        }

        private static (double[] point, double[] dir)? ResolveAxis(object entity, bool natural)
        {
            if (!(entity is WorkAxis axis))
            {
                return null;
            }

            double[] dir = V(axis.Line.Direction);
            if (!natural)
            {
                dir = new[] { -dir[0], -dir[1], -dir[2] };
            }

            return (P3(axis.Line.RootPoint), dir);
        }

        private static (double[] origin, double[] normal)? ResolvePlane(object entity)
        {
            switch (entity)
            {
                case WorkPlane wp:
                    return (P3(wp.Plane.RootPoint), V(wp.Plane.Normal));
                case Face face when face.Geometry is Plane plane:
                    return (P3(plane.RootPoint), V(plane.Normal));
                default:
                    return null;
            }
        }

        private static void AddSources(InventorReplicatingFeature feature, List<int> sources)
        {
            foreach (int s in sources)
            {
                feature.SourceFeatureIndices.Add(s);
            }
        }

        private static double[] Scale(double[] v, double s) => new[] { v[0] * s, v[1] * s, v[2] * s };

        private static double[] Sub(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };

        private static void ExtractRevolves(RevolveFeatures revolves, InventorDocument ir)
        {
            for (int i = 1; i <= revolves.Count; i++)
            {
                RevolveFeature rev = revolves[i];
                int sketchIndex = SketchIndexOf(ir, ((PlanarSketch)rev.Profile.Parent).Name);
                if (sketchIndex < 0)
                {
                    continue;
                }

                // Oblikovati revolves about the sketch's own centerline, so add the axis line to
                // the profile sketch as a centerline (its 2D endpoints come straight from the axis).
                InjectCenterline(ir.Sketches[sketchIndex], rev._AxisEntity);
                ir.Features.Add(new InventorRevolve
                {
                    Name = rev.Name,
                    SketchIndex = sketchIndex,
                    ProfileIndex = 0,
                    Operation = ToOperation(rev.Operation),
                    AngleRadians = rev.ExtentType == PartFeatureExtentEnum.kAngleExtent
                        ? ((AngleExtent)rev.Extent).Angle._Value
                        : 0, // full sweep
                });
            }
        }

        private static void InjectCenterline(InventorSketch sketch, SketchLine axis)
        {
            long nextId = 1;
            foreach (InventorCurve c in sketch.Curves)
            {
                if (c.Id >= nextId)
                {
                    nextId = c.Id + 1;
                }
            }

            sketch.Curves.Add(new InventorCurve
            {
                Id = nextId,
                Kind = InventorCurveKind.Line,
                Start = P2(axis.StartSketchPoint.Geometry),
                End = P2(axis.EndSketchPoint.Geometry),
                Centerline = true,
            });
        }

        private static double[] P2(Point2d p) => new[] { p.X, p.Y };

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
