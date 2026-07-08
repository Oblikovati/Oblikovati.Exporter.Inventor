// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// The dress-up emitters (fillet, chamfer, shell, draft). Each selects its edges/faces by
    /// GEOMETRY (the IR's midpoint+direction / centroid+normal descriptors), which the host rebinds
    /// to the built body every recompute — a located reference key would go stale once the base
    /// solid re-mints lineage, and this also sidesteps Inventor hiding the edges a dress-up consumed.
    /// A feature with no recorded edges/faces is deferred rather than emitted empty.
    /// </summary>
    public sealed class FilletEmitter : IFeatureEmitter
    {
        public bool Handles(InventorFeature feature) => feature is InventorFillet;

        public async Task<bool> EmitAsync(EmitContext context, InventorFeature feature, CancellationToken cancellationToken)
        {
            var fillet = (InventorFillet)feature;
            if (fillet.Edges.Count == 0)
            {
                context.Report.Deferrals.Add($"fillet '{fillet.Name}' has no recorded edges — deferred.");
                return false;
            }
            await context.AddFeatureAsync("fillet", new Dictionary<string, object?>
            {
                ["edgesGeom"] = GeomSelectors.Edges(fillet.Edges),
                ["radius"] = FeatureMapping.Millimeters(fillet.RadiusCm),
            }, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }

    /// <summary>Emits an <see cref="InventorChamfer"/>: bevels its geometrically-selected edges.</summary>
    public sealed class ChamferEmitter : IFeatureEmitter
    {
        public bool Handles(InventorFeature feature) => feature is InventorChamfer;

        public async Task<bool> EmitAsync(EmitContext context, InventorFeature feature, CancellationToken cancellationToken)
        {
            var chamfer = (InventorChamfer)feature;
            if (chamfer.Edges.Count == 0)
            {
                context.Report.Deferrals.Add($"chamfer '{chamfer.Name}' has no recorded edges — deferred.");
                return false;
            }
            await context.AddFeatureAsync("chamfer", new Dictionary<string, object?>
            {
                ["edgesGeom"] = GeomSelectors.Edges(chamfer.Edges),
                ["distance"] = FeatureMapping.Millimeters(chamfer.DistanceCm),
            }, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }

    /// <summary>Emits an <see cref="InventorShell"/>: hollows the body, removing its selected faces.</summary>
    public sealed class ShellEmitter : IFeatureEmitter
    {
        public bool Handles(InventorFeature feature) => feature is InventorShell;

        public async Task<bool> EmitAsync(EmitContext context, InventorFeature feature, CancellationToken cancellationToken)
        {
            var shell = (InventorShell)feature;
            await context.AddFeatureAsync("shell", new Dictionary<string, object?>
            {
                // An empty removed-face set is valid: a closed hollow (shell with no opening).
                ["facesGeom"] = GeomSelectors.Faces(shell.RemovedFaces),
                ["thickness"] = FeatureMapping.Millimeters(shell.ThicknessCm),
            }, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }

    /// <summary>Emits an <see cref="InventorDraft"/>: tapers its selected faces about the pull direction.</summary>
    public sealed class DraftEmitter : IFeatureEmitter
    {
        public bool Handles(InventorFeature feature) => feature is InventorDraft;

        public async Task<bool> EmitAsync(EmitContext context, InventorFeature feature, CancellationToken cancellationToken)
        {
            var draft = (InventorDraft)feature;
            if (draft.Faces.Count == 0)
            {
                context.Report.Deferrals.Add($"draft '{draft.Name}' has no recorded faces — deferred.");
                return false;
            }
            await context.AddFeatureAsync("draft", new Dictionary<string, object?>
            {
                ["facesGeom"] = GeomSelectors.Faces(draft.Faces),
                ["angle"] = FeatureMapping.Degrees(draft.AngleRadians),
                ["pullDirection"] = draft.Pull,
            }, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
