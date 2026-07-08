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
                default:
                    return false;
            }
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
