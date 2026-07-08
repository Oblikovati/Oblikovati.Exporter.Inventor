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
