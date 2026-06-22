// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>
    /// Records what the translator could and could not carry across. Unsupported Inventor
    /// features are appended here (never silently dropped, never STEP-substituted) so the
    /// user sees exactly what did not survive while the rest of the history exports intact.
    /// </summary>
    public sealed class ExportReport
    {
        private readonly List<string> _unsupported = new List<string>();

        /// <summary>Features/objects the translator skipped, in encounter order.</summary>
        public IReadOnlyList<string> Unsupported => _unsupported;

        /// <summary>
        /// Notes that <paramref name="what"/> (e.g. "iFeature 'rib1'") could not be
        /// translated and <paramref name="why"/> it was skipped.
        /// </summary>
        public void Skip(string what, string why) => _unsupported.Add($"{what}: {why}");
    }
}
