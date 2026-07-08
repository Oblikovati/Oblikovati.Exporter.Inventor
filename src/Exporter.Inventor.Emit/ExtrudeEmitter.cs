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

            // Prefer the host-side seed resolution (it runs on the SOLVED sketch, immune to the
            // emitter's arc-region containment guess). Only when the IR carries no seed do we fall
            // back to resolving a profile index emitter-side.
            int profileIndex = extrude.ProfileSeeds.Count > 0
                ? extrude.ProfileIndex
                : await ProfileResolver.ResolveAsync(context, extrude.SketchIndex, hostSketch.Value,
                    extrude.ProfileSeeds, extrude.ProfileIndex, cancellationToken).ConfigureAwait(false);
            await context.AddFeatureAsync("extrude", ExtrudeArgs(hostSketch.Value, profileIndex, extrude), cancellationToken).ConfigureAwait(false);
            return true;
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
            // Interior seed point(s) select the region(s) on the solved sketch (host-side), the
            // stable selector; the host prefers these over profileIndex.
            if (extrude.ProfileSeeds.Count > 0)
                args["profileSeeds"] = extrude.ProfileSeeds;
            if (extrude.ExtentKind == InventorExtentKind.Distance)
                args["distance"] = FeatureMapping.Millimeters(extrude.Distance);
            if (Math.Abs(extrude.TaperRadians) > 1e-9)
                args["taper"] = FeatureMapping.Degrees(extrude.TaperRadians);
            return args;
        }
    }
}
