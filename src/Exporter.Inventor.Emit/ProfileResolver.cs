// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Resolves which live-solved profile a profile-consuming feature (extrude, revolve) should
    /// select, so every such emitter shares one resolution: list the sketch's solved profiles,
    /// and — when the IR carries an interior seed — match the seed's region against them by
    /// (area, holes); otherwise fall back to the IR's precomputed index. The seed path is robust
    /// to the host's unpredictable region ordering, which is the point of the live pivot.
    /// </summary>
    /// <example>
    /// <code>
    /// int profile = await ProfileResolver.ResolveAsync(ctx, extrude.SketchIndex, hostSketch,
    ///     extrude.ProfileSeeds, extrude.ProfileIndex, ct);
    /// </code>
    /// </example>
    internal static class ProfileResolver
    {
        public static async Task<int> ResolveAsync(EmitContext context, int irSketchIndex, int hostSketch,
            IList<double[]> seeds, int profileIndex, CancellationToken ct)
        {
            var result = await context.Bridge.CallToolAsync("list_sketch_profiles",
                new Dictionary<string, object?> { ["sketchIndex"] = hostSketch }, ct).ConfigureAwait(false);
            IReadOnlyList<LiveProfile> profiles = ProfileList.Parse(result);
            if (seeds.Count == 0)
                return ProfileSelector.SelectByIndex(profiles, profileIndex);
            return SelectBySeed(context.Document.Sketches[irSketchIndex], seeds[0], profileIndex, profiles);
        }

        // Matches the seed's region (computed from the authored geometry) to a live profile; if the
        // geometry is not resolvable in this slice (e.g. an arc-bounded region), falls back to the
        // precomputed index.
        private static int SelectBySeed(InventorSketch sketch, double[] seed, int profileIndex, IReadOnlyList<LiveProfile> profiles)
        {
            try
            {
                RegionKey target = SketchGeometry.RegionForSeed(sketch, seed);
                return ProfileSelector.SelectByRegion(profiles, target);
            }
            catch (InvalidOperationException)
            {
                return ProfileSelector.SelectByIndex(profiles, profileIndex);
            }
        }
    }
}
