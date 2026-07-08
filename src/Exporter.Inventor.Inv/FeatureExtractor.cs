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
            // Work planes are not part of the Features history, so read them first (as fixed-frame
            // datums the later features may reference).
            ExtractWorkPlanes(definition.WorkPlanes, ir);

            // The flat Features collection enumerates in BUILD ORDER, so walk it once and dispatch
            // each feature to its single-feature extractor. This preserves the browser/build order
            // in ir.Features (the type-segregated collections destroyed it, emitting subtractive
            // cuts before the base solid). Patterns/mirror resolve their sources against the
            // features extracted so far — build order guarantees a source precedes its pattern.
            PartFeatures features = definition.Features;
            for (int i = 1; i <= features.Count; i++)
            {
                object feature = features[i];
                switch (feature)
                {
                    case ExtrudeFeature e: ExtractOneExtrude(e, ir); break;
                    case RevolveFeature r: ExtractOneRevolve(r, ir); break;
                    case RectangularPatternFeature p: ExtractOneRectangularPattern(p, ir); break;
                    case CircularPatternFeature p: ExtractOneCircularPattern(p, ir); break;
                    case MirrorFeature m: ExtractOneMirror(m, ir); break;
                    case LoftFeature l: ExtractOneLoft(l, ir); break;
                    case SweepFeature s: ExtractOneSweep(s, ir); break;
                    case HoleFeature h: DressUpExtractor.ExtractOneHole(h, ir); break;
                    case FilletFeature f: DressUpExtractor.ExtractOneFillet(f, ir); break;
                    case ChamferFeature c: DressUpExtractor.ExtractOneChamfer(c, ir); break;
                    case ShellFeature sh: DressUpExtractor.ExtractOneShell(sh, ir); break;
                    case FaceDraftFeature d: DressUpExtractor.ExtractOneDraft(d, ir); break;
                    default: break; // unsupported feature kind: skip (as before)
                }
            }
        }

        // Chordal tolerance (cm) for tessellating a curved path segment into the path polyline.
        private const double PathStrokeToleranceCm = 0.02;

        // Sweeps whose path is a chain of line/arc/spline segments: each path entity's 3D geometry
        // gives a segment (oriented by OpposedToSketchEntity) chained into the path polyline; curved
        // segments are tessellated via the curve evaluator. A path with an unknown entity is skipped.
        private static void ExtractOneSweep(SweepFeature sw, InventorDocument ir)
        {
            int sketchIndex = SketchIndexOf(ir, ((PlanarSketch)sw.Profile.Parent).Name);
            if (sketchIndex < 0)
            {
                return;
            }

            List<double[]>? path = BuildPath(sw.Path);
            if (path == null || path.Count < 2)
            {
                return;
            }

            var sweep = new InventorSweep
            {
                Name = sw.Name,
                ProfileSketchIndex = sketchIndex,
                ProfileIndex = 0,
                Operation = ToOperation(sw.Operation),
            };
            foreach (double[] p in path)
            {
                sweep.Path.Add(p);
            }

            ir.Features.Add(sweep);
        }

        private static List<double[]>? BuildPath(Path path)
        {
            var points = new List<double[]>();
            for (int i = 1; i <= path.Count; i++)
            {
                PathEntity entity = path[i];
                List<double[]>? segment = SegmentPoints(entity.SketchEntity);
                if (segment == null || segment.Count < 2)
                {
                    return null; // unknown path-segment type -> skip the sweep
                }

                if (entity.OpposedToSketchEntity)
                {
                    segment.Reverse();
                }

                AppendSegment(points, segment);
            }

            return points;
        }

        // The polyline points of one path segment in model space: a line is its two endpoints; an
        // arc/spline is tessellated through its curve evaluator within a chordal tolerance.
        private static List<double[]>? SegmentPoints(object sketchEntity)
        {
            switch (sketchEntity)
            {
                case SketchLine line:
                    LineSegment seg = line.Geometry3d;
                    return new List<double[]> { P3(seg.StartPoint), P3(seg.EndPoint) };
                case SketchArc arc:
                    return TryTessellate(arc.Geometry3d.Evaluator);
                case SketchSpline spline:
                    return TryTessellate(spline.Geometry3d.Evaluator);
                default:
                    return null;
            }
        }

        // Tessellates, tolerating an evaluator that rejects GetStrokes with a COM type-mismatch
        // (seen on some helical/spline paths); returns null so the caller skips the whole sweep,
        // matching the existing "unknown path-segment" behaviour rather than aborting the export.
        private static List<double[]>? TryTessellate(CurveEvaluator evaluator)
        {
            try
            {
                return Tessellate(evaluator);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return null;
            }
        }

        private static List<double[]> Tessellate(CurveEvaluator evaluator)
        {
            evaluator.GetParamExtents(out double min, out double max);
            evaluator.GetStrokes(min, max, PathStrokeToleranceCm, out int count, out double[] coords);
            var points = new List<double[]>(count);
            for (int k = 0; k < count; k++)
            {
                points.Add(new[] { coords[k * 3], coords[k * 3 + 1], coords[k * 3 + 2] });
            }

            return points;
        }

        // Chains a segment onto the path, dropping its first point when it coincides with the
        // running path's last point (the shared junction between consecutive segments).
        private static void AppendSegment(List<double[]> points, List<double[]> segment)
        {
            int start = points.Count > 0 && Coincident(points[points.Count - 1], segment[0]) ? 1 : 0;
            for (int k = start; k < segment.Count; k++)
            {
                points.Add(segment[k]);
            }
        }

        private static bool Coincident(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
            return (dx * dx) + (dy * dy) + (dz * dz) <= PathStrokeToleranceCm * PathStrokeToleranceCm;
        }

        // Lofts reference their section profiles' sketches by name; sweeps need the path polyline
        // evaluated (a later step), so only loft is read here.
        private static void ExtractOneLoft(LoftFeature l, InventorDocument ir)
        {
            var loft = new InventorLoft { Name = l.Name, Operation = ToOperation(l.Operation) };
            ObjectCollection sections = l.Sections;
            bool resolved = true;
            for (int j = 1; j <= sections.Count; j++)
            {
                if (!(sections[j] is Profile profile))
                {
                    resolved = false; // apex/point sections are a later step
                    break;
                }

                int idx = SketchIndexOf(ir, ((PlanarSketch)profile.Parent).Name);
                if (idx < 0)
                {
                    resolved = false;
                    break;
                }

                loft.Sections.Add(new InventorLoftSection { SketchIndex = idx, ProfileIndex = 0 });
            }

            if (resolved && loft.Sections.Count >= 2)
            {
                ir.Features.Add(loft);
            }
        }

        private static void ExtractOneRectangularPattern(RectangularPatternFeature p, InventorDocument ir)
        {
            double[]? xDir = ResolveDirection(p.XDirectionEntity, p.NaturalXDirection);
            if (xDir == null || !TryResolveSources(p.ParentFeatures, ir, out var sources))
            {
                return; // unresolved direction or source -> skip rather than guess
            }

            // A single-direction rectangular pattern leaves the Y parameters (YCount/YSpacing)
            // null, so read them only when a second direction is actually present.
            var pattern = new InventorRectangularPattern
            {
                Name = p.Name,
                CountX = (int)p.XCount._Value,
                CountY = p.YCount != null ? (int)p.YCount._Value : 1,
                StepX = Scale(xDir, p.XSpacing._Value),
            };
            // Only touch the Y-direction entities for a genuine two-direction pattern: a
            // single-direction pattern raises E_FAIL just accessing YDirectionEntity /
            // NaturalYDirection.
            if (pattern.CountY > 1)
            {
                double[]? yDir = ResolveDirection(p.YDirectionEntity, p.NaturalYDirection);
                if (yDir != null && p.YSpacing != null)
                {
                    pattern.StepY = Scale(yDir, p.YSpacing._Value);
                }
            }

            AddSources(pattern, sources);
            ir.Features.Add(pattern);
        }

        private static void ExtractOneCircularPattern(CircularPatternFeature p, InventorDocument ir)
        {
            (double[] point, double[] dir)? axis = ResolveAxis(p.AxisEntity, p.NaturalAxisDirection);
            if (axis == null || !TryResolveSources(p.ParentFeatures, ir, out var sources))
            {
                return;
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

        private static void ExtractOneMirror(MirrorFeature m, InventorDocument ir)
        {
            (double[] origin, double[] normal)? plane = ResolvePlane(m.MirrorPlaneEntity);
            if (plane == null || !TryResolveSources(m.ParentFeatures, ir, out var sources))
            {
                return;
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

        private static void ExtractOneRevolve(RevolveFeature rev, InventorDocument ir)
        {
            int sketchIndex = SketchIndexOf(ir, ((PlanarSketch)rev.Profile.Parent).Name);
            if (sketchIndex < 0)
            {
                return;
            }

            // Oblikovati revolves about a sketch centerline, so add the axis line to the profile
            // sketch as a centerline (its 2D endpoints come straight from the axis). Record the
            // centerline's line index so the recipe can name it explicitly — several revolves may
            // share one sketch, and then "the sketch's single centerline" is ambiguous.
            InventorSketch profileSketch = ir.Sketches[sketchIndex];
            int axisLineIndex = CountLines(profileSketch); // the index the appended centerline takes
            bool injected = InjectCenterline(profileSketch, rev._AxisEntity);
            var revolve = new InventorRevolve
            {
                Name = rev.Name,
                SketchIndex = sketchIndex,
                ProfileIndex = 0,
                Operation = ToOperation(rev.Operation),
                AngleRadians = rev.ExtentType == PartFeatureExtentEnum.kAngleExtent
                    ? ((AngleExtent)rev.Extent).Angle._Value
                    : 0, // full sweep
                AxisLineIndex = injected ? axisLineIndex : -1,
            };
            foreach (double[] seed in ProfileSeeds(rev.Profile))
            {
                revolve.ProfileSeeds.Add(seed);
            }

            ir.Features.Add(revolve);
        }

        // Adds the revolve axis to the profile sketch as a centerline line. Returns false (adds
        // nothing) when the axis has no readable 2D geometry, so the caller falls back to
        // own-centerline mode instead of naming a line that was never emitted.
        private static bool InjectCenterline(InventorSketch sketch, SketchLine axis)
        {
            // Read the axis from its 2D line geometry, not its sketch points: a revolve axis is
            // often a projected reference line whose Start/EndSketchPoint are null.
            LineSegment2d? geometry = axis?.Geometry;
            if (geometry == null)
            {
                return false;
            }

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
                Start = P2(geometry.StartPoint),
                End = P2(geometry.EndPoint),
                Centerline = true,
            });
            return true;
        }

        // The number of line-kind curves currently in the sketch — the 0-based line index the next
        // appended line will occupy. Mirrors the reader's Lines() ordering (line-kind entities in
        // recipe order), so it names the centerline the reader will resolve.
        private static int CountLines(InventorSketch sketch)
        {
            int n = 0;
            foreach (InventorCurve c in sketch.Curves)
            {
                if (c.Kind == InventorCurveKind.Line)
                {
                    n++;
                }
            }

            return n;
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

        private static void ExtractOneExtrude(ExtrudeFeature ext, InventorDocument ir)
        {
            PartFeatureExtent extent = ext.Definition.Extent;

            int sketchIndex = SketchIndexOf(ir, ((PlanarSketch)ext.Profile.Parent).Name);
            if (sketchIndex < 0)
            {
                return;
            }

            InventorExtrude? feature = null;
            if (extent is DistanceExtent distance)
            {
                feature = new InventorExtrude
                {
                    ExtentKind = InventorExtentKind.Distance,
                    Direction = ToDirection(distance.Direction),
                    Distance = distance.Distance._Value,
                };
            }
            else if (extent is ThroughAllExtent through)
            {
                // A through-all cut/join spans the existing material; the engine resolves the
                // span, so only the direction is needed. Dropping these was the biggest volume
                // gap (subtractive cuts vanished).
                feature = new InventorExtrude
                {
                    ExtentKind = InventorExtentKind.ThroughAll,
                    Direction = ToDirection(through.Direction),
                };
            }

            if (feature == null)
            {
                return; // to-face / from-to (need work-plane targets) are a later step
            }

            feature.Name = ext.Name;
            feature.SketchIndex = sketchIndex;
            feature.ProfileIndex = 0;
            feature.Operation = ToOperation(ext.Operation);
            foreach (double[] seed in ProfileSeeds(ext.Profile))
            {
                feature.ProfileSeeds.Add(seed);
            }

            ir.Features.Add(feature);

            // Author the exact selected profile loops (Profile.ProfilePaths) so the emitter can
            // reproduce the precise boundary; ProfileSeeds/SketchIndex above stay as the fallback.
            ProfileExtractor.Extract(ext, feature);
        }

        // One guaranteed-interior seed point (sketch cm) per region the feature's Profile
        // adds/removes. Inventor records the exact selected region(s) as ProfilePaths:
        // AddsMaterial=true paths are region outers, AddsMaterial=false paths are their holes. A
        // seed lets the reader pick the same region without depending on its region-ordering.
        private static IEnumerable<double[]> ProfileSeeds(Profile profile)
        {
            var outers = new List<List<double[]>>();
            var holes = new List<List<double[]>>();
            foreach (ProfilePath path in profile)
            {
                List<double[]> poly = LoopPolygon(path);
                if (poly.Count < 3)
                {
                    continue;
                }

                // A degenerate (near-zero-area) outer is not a real material region — Inventor can
                // include such a sliver path in a selected profile. Its interior seed would land on
                // the sliver, which lies inside a large enclosing region in the reader, so the reader
                // would fill that whole region and balloon the volume (TapePath: +1090%). Skip it.
                if (path.AddsMaterial && PolygonArea(poly) < MinRegionAreaCm2)
                {
                    continue;
                }

                (path.AddsMaterial ? outers : holes).Add(poly);
            }

            foreach (List<double[]> outer in outers)
            {
                var owned = new List<List<double[]>>();
                foreach (List<double[]> h in holes)
                {
                    if (h.Count > 0 && PointInPolygon(h[0], outer))
                    {
                        owned.Add(h);
                    }
                }

                double[]? seed = InteriorPoint(outer, owned);
                if (seed != null)
                {
                    yield return seed;
                }
            }
        }

        // Smallest area (cm²) an outer profile path must enclose to count as a real material
        // region. Below this it is a degenerate sliver whose seed would mis-resolve; genuine small
        // regions in the corpus are ~4e-3 cm², well above this floor.
        private const double MinRegionAreaCm2 = 1e-4;

        // The unsigned area of a closed polygon (shoelace), used to reject degenerate outers.
        private static double PolygonArea(List<double[]> poly)
        {
            double sum = 0;
            for (int i = 0, n = poly.Count; i < n; i++)
            {
                double[] a = poly[i], b = poly[(i + 1) % n];
                sum += a[0] * b[1] - b[0] * a[1];
            }

            return System.Math.Abs(sum) / 2.0;
        }

        // A ProfilePath's loop as a polygon of its entities' start points (sketch cm). Straight
        // edges are exact; a spline/arc edge is chorded (start-point sample) — adequate because
        // the interior seed is taken well inside the loop, not on its boundary.
        private static List<double[]> LoopPolygon(ProfilePath path)
        {
            var poly = new List<double[]>();
            foreach (ProfileEntity entity in path)
            {
                SketchPoint start = entity.StartSketchPoint;
                if (start != null)
                {
                    Point2d g = start.Geometry;
                    poly.Add(new[] { g.X, g.Y });
                }

                AppendCurveSamples(entity, poly);
            }

            return poly;
        }

        // For a curved boundary (arc/circle/spline/ellipse), append interior points along the
        // curve in loop order, so a region bounded by curves is approximated well enough that the
        // interior-seed scanline lands inside it. A straight edge adds nothing (its endpoints
        // already bound the polygon). Best-effort: an evaluator that rejects sampling leaves the
        // chord, matching the prior behaviour.
        private static void AppendCurveSamples(ProfileEntity entity, List<double[]> poly)
        {
            try
            {
                Curve2dEvaluator? evaluator = Evaluator2d(entity.Curve);
                if (evaluator == null)
                {
                    return;
                }

                evaluator.GetParamExtents(out double min, out double max);
                double span = max - min;
                var pars = new[] { min + span * 0.25, min + span * 0.5, min + span * 0.75 };
                if (entity.OpposedToSketchEntity)
                {
                    System.Array.Reverse(pars); // sketch-entity param runs opposite the loop
                }

                double[] pts = new double[0];
                evaluator.GetPointAtParam(ref pars, ref pts);
                for (int i = 0; i + 1 < pts.Length; i += 2)
                {
                    poly.Add(new[] { pts[i], pts[i + 1] });
                }
            }
            catch (System.Exception)
            {
                // curve won't sample (COM type-mismatch / bad param) -> keep the chord
            }
        }

        // The 2D evaluator of a profile entity's curve, or null for a straight segment (no sampling).
        private static Curve2dEvaluator? Evaluator2d(object curve) => curve switch
        {
            Arc2d arc => arc.Evaluator,
            Circle2d circle => circle.Evaluator,
            BSplineCurve2d spline => spline.Evaluator,
            EllipticalArc2d ellipticalArc => ellipticalArc.Evaluator,
            EllipseFull2d ellipse => ellipse.Evaluator,
            _ => null,
        };

        // A point strictly inside `outer` and outside every hole, via a horizontal scanline at the
        // loop's mean Y: even-odd crossings give the interior spans; return the midpoint of the
        // widest (robust for a concave region whose vertex centroid falls outside it).
        private static double[]? InteriorPoint(List<double[]> outer, List<List<double[]>> holes)
        {
            double y = 0;
            foreach (double[] p in outer) y += p[1];
            y /= outer.Count;

            var xs = new List<double>();
            Crossings(outer, y, xs);
            foreach (List<double[]> h in holes) Crossings(h, y, xs);
            xs.Sort();

            double bestMid = 0, bestWidth = -1;
            for (int i = 0; i + 1 < xs.Count; i += 2)
            {
                double w = xs[i + 1] - xs[i];
                if (w > bestWidth)
                {
                    bestWidth = w;
                    bestMid = (xs[i] + xs[i + 1]) / 2;
                }
            }

            return bestWidth > 1e-9 ? new[] { bestMid, y } : null;
        }

        private static void Crossings(List<double[]> poly, double y, List<double> xs)
        {
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                double[] a = poly[i], b = poly[(i + 1) % n];
                if ((a[1] <= y && b[1] > y) || (b[1] <= y && a[1] > y))
                {
                    double t = (y - a[1]) / (b[1] - a[1]);
                    xs.Add(a[0] + t * (b[0] - a[0]));
                }
            }
        }

        private static bool PointInPolygon(double[] q, List<double[]> poly)
        {
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double[] a = poly[i], b = poly[j];
                if (((a[1] > q[1]) != (b[1] > q[1])) &&
                    (q[0] < (b[0] - a[0]) * (q[1] - a[1]) / (b[1] - a[1]) + a[0]))
                {
                    inside = !inside;
                }
            }

            return inside;
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
        internal static (double[] X, double[] Y) AxesFromNormal(UnitVector normal)
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
