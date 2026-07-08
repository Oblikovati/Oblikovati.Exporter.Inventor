// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Bridge;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Authors one IR sketch on the live part: <c>create_sketch</c> on the mapped origin plane,
    /// then one <c>add_sketch_entity</c> per profile curve at its solved 2D position (cm). This
    /// slice emits geometry only — no constraints/dimensions — because the IR coordinates are
    /// already the final solved positions, so the profile (and therefore the extruded volume) is
    /// correct; parametric editability is a later refinement.
    /// </summary>
    internal sealed class SketchEmitter
    {
        private readonly BridgeClient _bridge;

        public SketchEmitter(BridgeClient bridge)
        {
            _bridge = bridge;
        }

        /// <summary>
        /// Creates the sketch and emits its curves; returns the host sketch index. Returns null (a
        /// deferral) when the sketch's plane can be mapped to neither an origin plane nor a
        /// fixed-frame work plane, or a curve kind is not yet supported.
        /// </summary>
        public async Task<int?> EmitAsync(InventorSketch sketch, IList<string> deferrals, CancellationToken ct)
        {
            int? index = await CreateSketchHostAsync(sketch, deferrals, ct).ConfigureAwait(false);
            if (!index.HasValue)
                return null;
            foreach (InventorCurve curve in sketch.Curves)
            {
                if (curve.Centerline)
                    continue; // an axis, not profile geometry
                if (!await EmitCurveAsync(index.Value, curve, ct).ConfigureAwait(false))
                {
                    deferrals.Add($"sketch '{sketch.Name}' curve kind {curve.Kind} is not supported in this slice — deferred.");
                    return null;
                }
            }
            return index;
        }

        // Creates the host sketch on the mapped plane: the fast path is an axis-aligned origin plane
        // (create_sketch {plane}); otherwise the sketch's datum frame is authored as a fixed-frame
        // work plane (create_work_plane) and the sketch is created on it (create_sketch
        // {workPlaneIndex}). A datum frame that maps to neither is deferred.
        private async Task<int?> CreateSketchHostAsync(InventorSketch sketch, IList<string> deferrals, CancellationToken ct)
        {
            if (PlaneMapper.TryMapOriginPlane(sketch, out string plane))
                return await CreateOnOriginPlaneAsync(plane, ct).ConfigureAwait(false);
            if (WorkPlaneMapper.TryMap(sketch, out WorkPlaneSpec spec))
                return await CreateOnWorkPlaneAsync(sketch, spec, deferrals, ct).ConfigureAwait(false);
            deferrals.Add($"sketch '{sketch.Name}' plane is neither an origin plane nor a mappable work plane — deferred.");
            return null;
        }

        private async Task<int> CreateOnOriginPlaneAsync(string plane, CancellationToken ct)
        {
            var result = await _bridge.CallToolAsync("create_sketch",
                new Dictionary<string, object?> { ["plane"] = plane }, ct).ConfigureAwait(false);
            return SketchIndexOf(result);
        }

        // Authors the datum as a fixed-frame work plane, then a sketch on it. An unhealthy work
        // plane (the constructor could not satisfy the frame) is deferred rather than sketched on.
        private async Task<int?> CreateOnWorkPlaneAsync(InventorSketch sketch, WorkPlaneSpec spec, IList<string> deferrals, CancellationToken ct)
        {
            var wp = await _bridge.CallToolAsync("create_work_plane", new Dictionary<string, object?>
            {
                ["kind"] = spec.Kind,
                ["origin"] = spec.Origin,
                ["xaxis"] = spec.XAxis,
                ["yaxis"] = spec.YAxis,
            }, ct).ConfigureAwait(false);
            if (!wp.TryGetProperty("healthy", out var h) || h.ValueKind != JsonValueKind.True ||
                !wp.TryGetProperty("index", out var wi) || !wi.TryGetInt32(out int workPlaneIndex))
            {
                deferrals.Add($"sketch '{sketch.Name}' work plane could not be built (unhealthy fixed frame) — deferred.");
                return null;
            }
            var result = await _bridge.CallToolAsync("create_sketch",
                new Dictionary<string, object?> { ["workPlaneIndex"] = workPlaneIndex }, ct).ConfigureAwait(false);
            return SketchIndexOf(result);
        }

        private static int SketchIndexOf(JsonElement result) =>
            result.TryGetProperty("sketchIndex", out var v) && v.TryGetInt32(out int i) ? i : 0;

        // Emits line/circle/arc; returns false for a kind this slice does not author.
        private async Task<bool> EmitCurveAsync(int sketchIndex, InventorCurve curve, CancellationToken ct)
        {
            switch (curve.Kind)
            {
                case InventorCurveKind.Line:
                    await AddEntityAsync(sketchIndex, "line", new[] { curve.Start, curve.End }, curve, null, ct).ConfigureAwait(false);
                    return true;
                case InventorCurveKind.Circle:
                    await AddEntityAsync(sketchIndex, "circle", new[] { curve.Center }, curve,
                        FeatureMapping.Millimeters(curve.Radius), ct).ConfigureAwait(false);
                    return true;
                case InventorCurveKind.Arc:
                    await AddEntityAsync(sketchIndex, "arc", new[] { curve.Center, curve.Start, curve.End }, curve, null, ct).ConfigureAwait(false);
                    return true;
                case InventorCurveKind.Ellipse:
                    await AddConicAsync(sketchIndex, "ellipse", curve, ct).ConfigureAwait(false);
                    return true;
                case InventorCurveKind.EllipticalArc:
                    await AddConicAsync(sketchIndex, "ellipticalArc", curve, ct).ConfigureAwait(false);
                    return true;
                case InventorCurveKind.Spline:
                    await AddSplineAsync(sketchIndex, curve, ct).ConfigureAwait(false);
                    return true;
                default:
                    return false;
            }
        }

        // Authors an ellipse or elliptical arc: centre + major-axis direction + the two radii, and
        // (for the arc) the sweep bounds. Radii are cm in the IR → millimetre expressions; angles
        // are radians → degree expressions, the forms the host schema parses. The arc's angles are
        // derived from its ENDPOINTS (see ArcAngles) so the host reproduces those exact points and
        // the loop closes against the adjacent curves — the sketch node-merge tolerance is 1e-6 cm,
        // far tighter than an angle-convention round-trip would guarantee.
        private async Task AddConicAsync(int sketchIndex, string kind, InventorCurve curve, CancellationToken ct)
        {
            var args = new Dictionary<string, object?>
            {
                ["sketchIndex"] = sketchIndex,
                ["kind"] = kind,
                ["points"] = new[] { curve.Center },
                ["axis"] = curve.MajorAxis,
                ["majorRadius"] = FeatureMapping.Millimeters(curve.MajorRadius),
                ["minorRadius"] = FeatureMapping.Millimeters(curve.MinorRadius),
            };
            if (kind == "ellipticalArc")
            {
                (double start, double end) = ArcAngles(curve);
                args["startAngle"] = FeatureMapping.Degrees(start);
                args["endAngle"] = FeatureMapping.Degrees(end);
            }
            if (curve.Construction)
                args["construction"] = true;
            await _bridge.CallToolAsync("add_sketch_entity", args, ct).ConfigureAwait(false);
        }

        // ArcAngles returns the host-frame parametric angles (radians) at the arc's start and end
        // POINTS, spanning the same portion of the ellipse Inventor's arc did. Anchoring on the
        // endpoints (rather than Inventor's own StartAngle/SweepAngle) makes the host's reconstructed
        // endpoints coincide with the adjacent curves regardless of any angle-convention difference.
        // Both endpoints leave two candidate sweeps (the minor and major arc); Inventor's sweep
        // MAGNITUDE picks which, so a convention sign flip cannot select the wrong portion.
        private static (double start, double end) ArcAngles(InventorCurve curve)
        {
            const double twoPi = 2.0 * System.Math.PI;
            double start = EllipseAngle(curve.Start, curve);
            double end = EllipseAngle(curve.End, curve);
            double ccwDist = end - start;
            while (ccwDist <= 0) ccwDist += twoPi; // CCW angular distance start→end, in (0, 2π]
            double target = System.Math.Abs(curve.EndAngle - curve.StartAngle); // Inventor's swept magnitude
            while (target > twoPi) target -= twoPi;
            // Take the CCW arc when its span matches Inventor's magnitude better than the CW arc's.
            if (System.Math.Abs(ccwDist - target) <= System.Math.Abs((twoPi - ccwDist) - target))
                return (start, start + ccwDist);
            return (start, start - (twoPi - ccwDist));
        }

        // EllipseAngle is the parametric angle a of point p on the ellipse, i.e. the a for which the
        // host's point formula center + Rmaj·cos(a)·major + Rmin·sin(a)·minorPerp equals p, where
        // minorPerp is major rotated +90°. a = atan2(v/Rmin, u/Rmaj) with (u,v) = p−center resolved
        // onto (major, minorPerp).
        private static double EllipseAngle(double[] p, InventorCurve curve)
        {
            double[] c = curve.Center, m = curve.MajorAxis;
            double dx = p[0] - c[0], dy = p[1] - c[1];
            double u = dx * m[0] + dy * m[1];    // along major
            double v = -dx * m[1] + dy * m[0];   // along minorPerp (major rotated +90°)
            return System.Math.Atan2(v / curve.MinorRadius, u / curve.MajorRadius);
        }

        // Authors a spline through its captured points. A FIT spline interpolates the points
        // ("spline"); otherwise the points are control points ("controlPointSpline"). Closed marks a
        // periodic loop. The points are the solved 2D positions, so the curve reproduces the region.
        private async Task AddSplineAsync(int sketchIndex, InventorCurve curve, CancellationToken ct)
        {
            var args = new Dictionary<string, object?>
            {
                ["sketchIndex"] = sketchIndex,
                ["kind"] = curve.Fit ? "spline" : "controlPointSpline",
                ["points"] = curve.SplinePoints,
                ["closed"] = curve.Closed,
            };
            if (curve.Construction)
                args["construction"] = true;
            await _bridge.CallToolAsync("add_sketch_entity", args, ct).ConfigureAwait(false);
        }

        private async Task AddEntityAsync(int sketchIndex, string kind, double[][] points, InventorCurve curve, string? radius, CancellationToken ct)
        {
            var args = new Dictionary<string, object?>
            {
                ["sketchIndex"] = sketchIndex,
                ["kind"] = kind,
                ["points"] = points,
            };
            if (radius != null)
                args["radius"] = radius;
            if (kind == "arc")
                args["ccw"] = curve.Ccw;
            if (curve.Construction)
                args["construction"] = true;
            await _bridge.CallToolAsync("add_sketch_entity", args, ct).ConfigureAwait(false);
        }
    }
}
