// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Emits an <see cref="InventorRevolve"/>: maps its axis to an origin work-axis ref, authors its
    /// profile sketch, resolves the target profile LIVE against the solved regions, and adds a
    /// <c>revolve</c> feature. A revolve whose axis is not a global origin axis is deferred (there is
    /// no bridge tool to build a work axis from a sketch line — see <see cref="RevolveAxis"/>).
    /// </summary>
    public sealed class RevolveEmitter : IFeatureEmitter
    {
        public bool Handles(InventorFeature feature) => feature is InventorRevolve;

        public async Task<bool> EmitAsync(EmitContext context, InventorFeature feature, CancellationToken cancellationToken)
        {
            var revolve = (InventorRevolve)feature;
            InventorSketch sketch = context.Document.Sketches[revolve.SketchIndex];
            if (!RevolveAxis.TryResolve(sketch, revolve, out string axisRef))
            {
                context.Report.Deferrals.Add(
                    $"revolve '{revolve.Name}' axis is not a global origin axis (offset/tilted centerline) — deferred.");
                return false;
            }

            int? hostSketch = await context.EnsureSketchAsync(revolve.SketchIndex, cancellationToken).ConfigureAwait(false);
            if (!hostSketch.HasValue)
                return false; // sketch deferred; reason already on the report

            int profileIndex = await ProfileResolver.ResolveAsync(context, revolve.SketchIndex, hostSketch.Value,
                revolve.ProfileSeeds, revolve.ProfileIndex, cancellationToken).ConfigureAwait(false);
            await context.Bridge.CallToolAsync("add_feature", new Dictionary<string, object?>
            {
                ["kind"] = "revolve",
                ["args"] = RevolveArgs(hostSketch.Value, profileIndex, revolve, axisRef),
            }, cancellationToken).ConfigureAwait(false);
            return true;
        }

        private static Dictionary<string, object?> RevolveArgs(int sketchIndex, int profileIndex, InventorRevolve revolve, string axisRef) =>
            new Dictionary<string, object?>
            {
                ["sketchIndex"] = sketchIndex,
                ["profileIndex"] = profileIndex,
                ["axisRef"] = axisRef,
                ["angle"] = FeatureMapping.RevolveAngle(revolve.AngleRadians),
                ["operation"] = FeatureMapping.Operation(revolve.Operation),
            };
    }
}
