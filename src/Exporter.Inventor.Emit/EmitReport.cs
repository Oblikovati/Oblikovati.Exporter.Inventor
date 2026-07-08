// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// The outcome of emitting one <see cref="Model.InventorDocument"/> to the bridge: the created
    /// document's name/id, how many sketches and features were emitted, and what was deferred (a
    /// feature kind or a sketch plane this slice does not handle yet). Deferrals are recorded, not
    /// thrown, so a partial part still builds and the caller sees exactly what was skipped.
    /// </summary>
    public sealed class EmitReport
    {
        public string DocumentName { get; set; } = string.Empty;

        public long DocumentId { get; set; }

        public int SketchesEmitted { get; set; }

        public int FeaturesEmitted { get; set; }

        /// <summary>Human-readable reasons work was skipped (unsupported feature kind / sketch plane).</summary>
        public IList<string> Deferrals { get; } = new List<string>();
    }
}
