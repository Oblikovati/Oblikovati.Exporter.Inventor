// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Emits an <see cref="InventorRevolve"/>: maps its axis to a work-axis ref, authors its profile
    /// sketch, resolves the target profile LIVE against the solved regions, and adds a <c>revolve</c>
    /// feature. A global origin centerline maps to an origin-axis ref directly; an offset/tilted
    /// centerline is built as a grounded work axis via <c>create_work_axis</c> (line kind) and the
    /// revolve turns about the returned ref. Deferred only when no axis line can be resolved at all.
    /// </summary>
    public sealed class RevolveEmitter : IFeatureEmitter
    {
        public bool Handles(InventorFeature feature) => feature is InventorRevolve;

        public async Task<bool> EmitAsync(EmitContext context, InventorFeature feature, CancellationToken cancellationToken)
        {
            var revolve = (InventorRevolve)feature;
            InventorSketch sketch = context.Document.Sketches[revolve.SketchIndex];

            string? axisRef = await ResolveAxisRefAsync(context, sketch, revolve, cancellationToken).ConfigureAwait(false);
            if (axisRef == null)
            {
                context.Report.Deferrals.Add($"revolve '{revolve.Name}' has no resolvable axis centerline — deferred.");
                return false;
            }

            // Prefer authoring Inventor's actual resolved profile loops (see ExtrudeEmitter); the axis
            // is resolved separately from the original sketch's centerline above, so it is unaffected.
            bool useProfileLoops = revolve.ProfileLoops.Count > 0;
            int? hostSketch = useProfileLoops
                ? await context.EmitProfileSketchAsync(revolve.SketchIndex, revolve.ProfileLoops, cancellationToken).ConfigureAwait(false)
                : await context.EnsureSketchAsync(revolve.SketchIndex, cancellationToken).ConfigureAwait(false);
            if (!hostSketch.HasValue)
                return false; // sketch deferred; reason already on the report

            int profileIndex = (useProfileLoops || revolve.ProfileSeeds.Count > 0)
                ? revolve.ProfileIndex
                : await ProfileResolver.ResolveAsync(context, revolve.SketchIndex, hostSketch.Value,
                    revolve.ProfileSeeds, revolve.ProfileIndex, cancellationToken).ConfigureAwait(false);
            await context.AddFeatureAsync("revolve", RevolveArgs(hostSketch.Value, profileIndex, revolve, axisRef), cancellationToken).ConfigureAwait(false);
            return true;
        }

        // An origin centerline maps straight to "origin/axis/x|y|z"; any other centerline is built as
        // a grounded work axis from its model-space line. Null ⇒ no axis line resolved.
        private static async Task<string?> ResolveAxisRefAsync(EmitContext context, InventorSketch sketch, InventorRevolve revolve, CancellationToken ct)
        {
            if (RevolveAxis.TryResolve(sketch, revolve, out string originRef))
                return originRef;
            if (RevolveAxis.TryModelAxis(sketch, revolve, out double[] origin, out double[] direction))
                return await CreateWorkAxisAsync(context, origin, direction, ct).ConfigureAwait(false);
            return null;
        }

        // Builds a grounded (line-kind) work axis from origin + direction and returns its ref
        // (e.g. "axis/1"); null when the host reports no usable ref.
        private static async Task<string?> CreateWorkAxisAsync(EmitContext context, double[] origin, double[] direction, CancellationToken ct)
        {
            JsonElement result = await context.Bridge.CallToolAsync("create_work_axis", new Dictionary<string, object?>
            {
                ["kind"] = "line",
                ["origin"] = origin,
                ["direction"] = direction,
            }, ct).ConfigureAwait(false);
            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("ref", out JsonElement r) &&
                r.ValueKind == JsonValueKind.String)
            {
                string s = r.GetString() ?? string.Empty;
                return s.Length == 0 ? null : s;
            }
            return null;
        }

        private static Dictionary<string, object?> RevolveArgs(int sketchIndex, int profileIndex, InventorRevolve revolve, string axisRef)
        {
            var args = new Dictionary<string, object?>
            {
                ["sketchIndex"] = sketchIndex,
                ["profileIndex"] = profileIndex,
                ["axisRef"] = axisRef,
                ["angle"] = FeatureMapping.RevolveAngle(revolve.AngleRadians),
                ["operation"] = FeatureMapping.Operation(revolve.Operation),
            };
            // The interior seed (first, one region per revolve) selects the region host-side on the
            // solved sketch, preferred over profileIndex.
            if (revolve.ProfileSeeds.Count > 0)
                args["profileSeed"] = revolve.ProfileSeeds[0];
            return args;
        }
    }
}
