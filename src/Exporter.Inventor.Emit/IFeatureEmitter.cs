// SPDX-License-Identifier: GPL-2.0-only

using System.Threading;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Emits one kind of IR feature to the live bridge. This is the seam later slices extend:
    /// each new feature kind (revolve, hole, pattern, …) ships its own emitter and is registered
    /// with <see cref="DocumentEmitter"/>, without touching the ones already proven.
    /// </summary>
    public interface IFeatureEmitter
    {
        /// <summary>Whether this emitter handles the given feature.</summary>
        bool Handles(InventorFeature feature);

        /// <summary>
        /// Emits the feature. Returns true when a feature was added; false when it was deferred
        /// (e.g. its sketch could not be authored) — the reason is recorded on the report.
        /// </summary>
        Task<bool> EmitAsync(EmitContext context, InventorFeature feature, CancellationToken cancellationToken);
    }
}
