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

            // Prefer authoring Inventor's ACTUAL resolved profile (its ProfilePaths) into a dedicated
            // sketch: it excludes projected reference geometry and includes exactly the boundary, so
            // the sketch's regions ARE the feature's profile and the seeds select reliably. Fall back
            // to the whole shared sketch + seeds when no profile loops were captured.
            bool useProfileLoops = extrude.ProfileLoops.Count > 0;
            int? hostSketch = useProfileLoops
                ? await context.EmitProfileSketchAsync(extrude.SketchIndex, extrude.ProfileLoops, cancellationToken).ConfigureAwait(false)
                : await context.EnsureSketchAsync(extrude.SketchIndex, cancellationToken).ConfigureAwait(false);
            if (!hostSketch.HasValue)
                return false; // sketch deferred; reason already on the report

            // Host-side seed resolution runs on the SOLVED sketch; when the IR carries no seed and no
            // profile loops, fall back to resolving a profile index emitter-side.
            int profileIndex = (useProfileLoops || extrude.ProfileSeeds.Count > 0)
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
            // A to-face extent names its planar stop face by geometry (the host has no key from us):
            // a point on the face + its normal, which the host binds to the current body's face.
            if (extrude.ExtentKind == InventorExtentKind.ToFace && extrude.ToFaceCentroid != null && extrude.ToFaceNormal != null)
                args["toFaceGeom"] = new Dictionary<string, object?>
                {
                    ["centroid"] = extrude.ToFaceCentroid,
                    ["normal"] = extrude.ToFaceNormal,
                };
            if (Math.Abs(extrude.TaperRadians) > 1e-9)
                args["taper"] = FeatureMapping.Degrees(extrude.TaperRadians);
            return args;
        }
    }
}
