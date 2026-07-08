// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Bridge;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Rebuilds an <see cref="InventorDocument"/> live over the MCP bridge by replaying its feature
    /// history as bridge calls. This slice covers sketches (line/circle/arc) + EXTRUDE; other
    /// feature kinds are deferred and recorded on the <see cref="EmitReport"/>. Each feature kind is
    /// handled by an <see cref="IFeatureEmitter"/> so later slices extend the surface without
    /// touching what is proven.
    /// </summary>
    /// <example>
    /// <code>
    /// await using var bridge = await BridgeClient.ConnectAsync();
    /// EmitReport report = await new DocumentEmitter().EmitAsync(bridge, doc);
    /// </code>
    /// </example>
    public sealed class DocumentEmitter
    {
        private readonly IReadOnlyList<IFeatureEmitter> _emitters;

        /// <summary>Uses the default emitter set (extrude only, this slice).</summary>
        public DocumentEmitter() : this(new IFeatureEmitter[] { new ExtrudeEmitter() })
        {
        }

        /// <summary>Uses a custom emitter set — the extension point for later feature slices.</summary>
        public DocumentEmitter(IReadOnlyList<IFeatureEmitter> emitters)
        {
            _emitters = emitters ?? throw new ArgumentNullException(nameof(emitters));
        }

        /// <summary>
        /// Creates a fresh part document (its name defaults to a unique name derived from the IR
        /// display name) and emits the document's features, returning what was built and deferred.
        /// </summary>
        public async Task<EmitReport> EmitAsync(BridgeClient bridge, InventorDocument document, string? documentName = null, CancellationToken cancellationToken = default)
        {
            if (bridge == null) throw new ArgumentNullException(nameof(bridge));
            if (document == null) throw new ArgumentNullException(nameof(document));

            var report = new EmitReport { DocumentName = documentName ?? UniqueName(document.DisplayName) };
            report.DocumentId = await CreateDocumentAsync(bridge, report.DocumentName, cancellationToken).ConfigureAwait(false);

            var context = new EmitContext(bridge, document, report);
            foreach (InventorFeature feature in document.Features)
                await EmitFeatureAsync(context, feature, report, cancellationToken).ConfigureAwait(false);
            return report;
        }

        private async Task EmitFeatureAsync(EmitContext context, InventorFeature feature, EmitReport report, CancellationToken ct)
        {
            IFeatureEmitter? emitter = FindEmitter(feature);
            if (emitter == null)
            {
                report.Deferrals.Add($"feature '{feature.Name}' of kind {feature.GetType().Name} is not supported in this slice — deferred.");
                return;
            }
            if (await emitter.EmitAsync(context, feature, ct).ConfigureAwait(false))
                report.FeaturesEmitted++;
        }

        private IFeatureEmitter? FindEmitter(InventorFeature feature)
        {
            foreach (IFeatureEmitter emitter in _emitters)
            {
                if (emitter.Handles(feature))
                    return emitter;
            }
            return null;
        }

        private static async Task<long> CreateDocumentAsync(BridgeClient bridge, string name, CancellationToken ct)
        {
            JsonElement created = await bridge.CallToolAsync("create_document",
                new Dictionary<string, object?> { ["type"] = "part", ["name"] = name }, ct).ConfigureAwait(false);
            return ReadDocumentId(created);
        }

        // The bridge returns the created document's numeric id under one of these field spellings.
        private static long ReadDocumentId(JsonElement created)
        {
            foreach (string name in new[] { "document", "documentId", "id" })
            {
                if (created.ValueKind == JsonValueKind.Object && created.TryGetProperty(name, out JsonElement v) &&
                    v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out long id))
                {
                    return id;
                }
            }
            return 0; // id is not required for this slice's volume acceptance; save is a later concern
        }

        // A per-run unique name so the host session (which persists across MCP connections) never
        // collides on a repeated document name.
        private static string UniqueName(string displayName)
        {
            string stem = string.IsNullOrWhiteSpace(displayName) ? "part" : displayName;
            return stem + "_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
