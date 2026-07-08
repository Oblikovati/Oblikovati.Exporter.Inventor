// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Emits an <see cref="InventorExtrude"/>: authors its sketch, resolves the target profile
    /// LIVE against the solved regions (the point of the pivot), and adds an <c>extrude</c> feature.
    /// </summary>
    public sealed class ExtrudeEmitter : IFeatureEmitter
    {
        public bool Handles(InventorFeature feature) => feature is InventorExtrude;

        public async Task<bool> EmitAsync(EmitContext context, InventorFeature feature, CancellationToken cancellationToken)
        {
            var extrude = (InventorExtrude)feature;
            int? hostSketch = await context.EnsureSketchAsync(extrude.SketchIndex, cancellationToken).ConfigureAwait(false);
            if (!hostSketch.HasValue)
                return false; // sketch deferred; reason already on the report

            int profileIndex = await ResolveProfileAsync(context, extrude, hostSketch.Value, cancellationToken).ConfigureAwait(false);
            await context.Bridge.CallToolAsync("add_feature", new Dictionary<string, object?>
            {
                ["kind"] = "extrude",
                ["args"] = ExtrudeArgs(hostSketch.Value, profileIndex, extrude),
            }, cancellationToken).ConfigureAwait(false);
            return true;
        }

        // Resolves the profile against the live-solved regions: by seed containment when the IR
        // carries a seed, else by the IR's precomputed index (validated against the solved set).
        private static async Task<int> ResolveProfileAsync(EmitContext context, InventorExtrude extrude, int hostSketch, CancellationToken ct)
        {
            var result = await context.Bridge.CallToolAsync("list_sketch_profiles",
                new Dictionary<string, object?> { ["sketchIndex"] = hostSketch }, ct).ConfigureAwait(false);
            IReadOnlyList<LiveProfile> profiles = ProfileList.Parse(result);
            if (extrude.ProfileSeeds.Count == 0)
                return ProfileSelector.SelectByIndex(profiles, extrude.ProfileIndex);
            return SelectBySeed(context.Document.Sketches[extrude.SketchIndex], extrude, profiles);
        }

        // Matches the seed's region (computed from the authored geometry) to a live profile; if the
        // geometry is not resolvable in this slice, falls back to the precomputed index.
        private static int SelectBySeed(InventorSketch sketch, InventorExtrude extrude, IReadOnlyList<LiveProfile> profiles)
        {
            try
            {
                RegionKey target = SketchGeometry.RegionForSeed(sketch, extrude.ProfileSeeds[0]);
                return ProfileSelector.SelectByRegion(profiles, target);
            }
            catch (InvalidOperationException)
            {
                return ProfileSelector.SelectByIndex(profiles, extrude.ProfileIndex);
            }
        }

        private static Dictionary<string, object?> ExtrudeArgs(int sketchIndex, int profileIndex, InventorExtrude extrude)
        {
            var args = new Dictionary<string, object?>
            {
                ["sketchIndex"] = sketchIndex,
                ["profileIndex"] = profileIndex,
                ["operation"] = FeatureMapping.Operation(extrude.Operation),
                ["extent"] = FeatureMapping.Extent(extrude.ExtentKind),
                ["direction"] = FeatureMapping.Direction(extrude.Direction),
            };
            if (extrude.ExtentKind == InventorExtentKind.Distance)
                args["distance"] = FeatureMapping.Millimeters(extrude.Distance);
            if (Math.Abs(extrude.TaperRadians) > 1e-9)
                args["taper"] = FeatureMapping.Degrees(extrude.TaperRadians);
            return args;
        }
    }
}
