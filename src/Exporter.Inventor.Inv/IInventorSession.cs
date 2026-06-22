// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Inv
{
    /// <summary>
    /// The thin seam over a live Inventor session. The orchestrator depends on this, not on
    /// the Inventor API, so it can be driven by a fake in tests. The production implementation
    /// is <see cref="InventorSessionAdapter"/>.
    /// </summary>
    public interface IInventorSession
    {
        /// <summary>
        /// Reads the active part/assembly into the Inventor-neutral IR. Throws if no document
        /// is open.
        /// </summary>
        InventorDocument ExtractActiveDocument();

        /// <summary>Directory the exported document(s) should be written next to.</summary>
        string OutputDirectory();

        /// <summary>Surfaces a short summary to the user (Inventor status bar).</summary>
        void ShowMessage(string message);
    }
}
