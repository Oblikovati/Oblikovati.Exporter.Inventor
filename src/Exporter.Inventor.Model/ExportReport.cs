// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.Inventor.Model
{
    /// <summary>
    /// Records what the export could and could not carry across, spanning both phases: the
    /// extraction of the Inventor document into the IR and the translation of that IR into the
    /// recipe. Unsupported or unresolvable items are appended here (never silently dropped, never
    /// STEP-substituted) so the user sees exactly what did not survive while the rest exports
    /// intact. It lives in the shared model layer so the extractor (.Inv) and the translator
    /// (.Translate) can write to the same ledger.
    /// </summary>
    public sealed class ExportReport
    {
        private readonly List<string> _unsupported = new List<string>();

        /// <summary>Features/objects the export skipped, in encounter order.</summary>
        public IReadOnlyList<string> Unsupported => _unsupported;

        /// <summary>
        /// Notes that <paramref name="what"/> (e.g. "iFeature 'rib1'") could not be
        /// carried across and <paramref name="why"/> it was skipped.
        /// </summary>
        public void Skip(string what, string why) => _unsupported.Add($"{what}: {why}");
    }
}
