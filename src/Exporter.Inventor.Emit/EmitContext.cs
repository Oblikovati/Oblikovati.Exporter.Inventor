// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Bridge;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// The per-run state a feature emitter needs: the live <see cref="BridgeClient"/>, the source
    /// <see cref="InventorDocument"/>, and the mapping from an IR sketch index to the host sketch
    /// index. Sketches are emitted lazily — the first feature that references a sketch authors it,
    /// so a sketch no emitted feature uses is never built — and memoized so shared sketches emit
    /// once.
    /// </summary>
    public sealed class EmitContext
    {
        private readonly SketchEmitter _sketchEmitter;
        private readonly Dictionary<int, int?> _hostSketchByIr = new Dictionary<int, int?>();
        private readonly Dictionary<int, string> _hostFeatureNameByIr = new Dictionary<int, string>();

        public EmitContext(BridgeClient bridge, InventorDocument document, EmitReport report)
        {
            Bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Report = report ?? throw new ArgumentNullException(nameof(report));
            _sketchEmitter = new SketchEmitter(bridge);
        }

        public BridgeClient Bridge { get; }

        public InventorDocument Document { get; }

        public EmitReport Report { get; }

        /// <summary>
        /// The index (into <see cref="Model.InventorDocument.Features"/>) of the feature currently
        /// being emitted. <see cref="DocumentEmitter"/> sets it before each emit so a created feature's
        /// host name can be recorded against its IR index (for a later pattern/mirror to reference).
        /// -1 ⇒ not tracking (e.g. a helper feature).
        /// </summary>
        public int CurrentFeatureIndex { get; set; } = -1;

        /// <summary>
        /// Adds a feature via the <c>add_feature</c> tool and returns the host's parsed reply. Records
        /// the created feature's name against <see cref="CurrentFeatureIndex"/> (so a pattern can name
        /// it later) and, when the host reports the feature unhealthy, appends the reason to
        /// <see cref="EmitReport.Warnings"/> — the emitter stays thin, one place owns the reply.
        /// </summary>
        public async Task<FeatureAddResult> AddFeatureAsync(string kind, IReadOnlyDictionary<string, object?> args, CancellationToken ct)
        {
            System.Text.Json.JsonElement raw = await Bridge.CallToolAsync("add_feature", new Dictionary<string, object?>
            {
                ["kind"] = kind,
                ["args"] = args,
            }, ct).ConfigureAwait(false);
            FeatureAddResult result = FeatureAddResult.Parse(raw);
            if (CurrentFeatureIndex >= 0 && result.Name.Length > 0)
                _hostFeatureNameByIr[CurrentFeatureIndex] = result.Name;
            if (!result.Healthy)
                Report.Warnings.Add($"feature '{result.Name}' ({kind}) is unhealthy: {result.Reason}");
            return result;
        }

        /// <summary>
        /// Resolves IR feature indices (a replicating feature's sources) to the host feature names
        /// recorded when they were emitted. Returns null when any source was not emitted (deferred),
        /// so the caller defers the replicating feature rather than pattern a phantom.
        /// </summary>
        public IReadOnlyList<string>? HostFeatureNames(IEnumerable<int> irFeatureIndices)
        {
            var names = new List<string>();
            foreach (int i in irFeatureIndices)
            {
                if (!_hostFeatureNameByIr.TryGetValue(i, out string? name))
                    return null;
                names.Add(name);
            }
            return names;
        }

        /// <summary>
        /// Authors a fresh, dedicated sketch for a feature's resolved profile loops on the plane of
        /// IR sketch <paramref name="planeSketchIndex"/>, returning its host index. Not cached — each
        /// feature gets its own profile sketch (the loops are that feature's, not the shared sketch).
        /// Null ⇒ deferred (unmappable plane / unsupported curve).
        /// </summary>
        public async Task<int?> EmitProfileSketchAsync(int planeSketchIndex, IList<InventorProfileLoop> loops, CancellationToken ct)
        {
            if (planeSketchIndex < 0 || planeSketchIndex >= Document.Sketches.Count)
                throw new InvalidOperationException($"feature references sketch {planeSketchIndex}, which does not exist.");
            InventorSketch planeSketch = Document.Sketches[planeSketchIndex];
            int? host = await _sketchEmitter.EmitProfileAsync(planeSketch, loops, Report.Deferrals, ct).ConfigureAwait(false);
            if (host.HasValue)
                Report.SketchesEmitted++;
            return host;
        }

        /// <summary>
        /// Returns the host sketch index for the given IR sketch, authoring it on first use. Returns
        /// null when the sketch was deferred (unsupported plane/curve); the reason is on the report.
        /// </summary>
        public async Task<int?> EnsureSketchAsync(int irSketchIndex, CancellationToken ct)
        {
            if (_hostSketchByIr.TryGetValue(irSketchIndex, out int? cached))
                return cached;
            if (irSketchIndex < 0 || irSketchIndex >= Document.Sketches.Count)
                throw new InvalidOperationException($"feature references sketch {irSketchIndex}, which does not exist.");

            InventorSketch sketch = Document.Sketches[irSketchIndex];
            int? host = await _sketchEmitter.EmitAsync(sketch, Report.Deferrals, ct).ConfigureAwait(false);
            if (host.HasValue)
                Report.SketchesEmitted++;
            _hostSketchByIr[irSketchIndex] = host;
            return host;
        }
    }
}
