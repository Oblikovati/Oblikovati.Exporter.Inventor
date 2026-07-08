// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Emits an <see cref="InventorHole"/>: drills a hole placed on the recorded face, selected by
    /// GEOMETRY (its centroid + normal) so the host rebinds it to the built body every recompute —
    /// a located reference key would go stale when the base solid re-mints lineage, leaving the hole
    /// cutting nothing. A through-all hole omits <c>depth</c>; a blind hole passes its depth.
    /// </summary>
    public sealed class HoleEmitter : IFeatureEmitter
    {
        public bool Handles(InventorFeature feature) => feature is InventorHole;

        public async Task<bool> EmitAsync(EmitContext context, InventorFeature feature, CancellationToken cancellationToken)
        {
            var hole = (InventorHole)feature;
            await context.AddFeatureAsync("hole", HoleArgs(hole), cancellationToken).ConfigureAwait(false);
            return true;
        }

        private static Dictionary<string, object?> HoleArgs(InventorHole hole)
        {
            var args = new Dictionary<string, object?>
            {
                ["placementFaceGeom"] = GeomSelectors.Face(hole.PlacementFace),
                ["diameter"] = FeatureMapping.Millimeters(hole.DiameterCm),
            };
            // Through-all omits depth (drills through the material); a blind hole ships its depth.
            if (!hole.ThroughAll)
                args["depth"] = FeatureMapping.Millimeters(hole.DepthCm);
            // The explicit drill centre (model-space cm) places holes off the face centroid — e.g. a
            // bolt circle. Null ⇒ the host drills at the placement face's centroid.
            if (hole.Center != null && hole.Center.Length == 3)
                args["center"] = hole.Center;
            return args;
        }
    }
}
