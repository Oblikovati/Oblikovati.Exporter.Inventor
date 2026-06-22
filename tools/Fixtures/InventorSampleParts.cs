// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Fixtures
{
    /// <summary>
    /// Representative Inventor-neutral documents, shared by the unit tests and the golden
    /// round-trip so both assert and open the exact same inputs. M1 covers the document
    /// envelope and model parameters; sketch/feature fixtures arrive in later milestones.
    /// </summary>
    public static class InventorSampleParts
    {
        /// <summary>A bare part: just the envelope and units. The smallest valid .opd.</summary>
        public static InventorDocument EmptyPart() => new InventorDocument
        {
            DisplayName = "empty",
            Kind = InventorDocumentKind.Part,
        };

        /// <summary>
        /// A part carrying parameters with a formula reference ("height = width * 2"),
        /// exercising inline units and cross-parameter references through the emitter.
        /// </summary>
        public static InventorDocument ParametricPart()
        {
            var doc = new InventorDocument
            {
                DisplayName = "parametric",
                Kind = InventorDocumentKind.Part,
            };
            doc.Parameters.Add(new InventorParameter { Name = "width", Expression = "40 mm", Unit = "mm" });
            doc.Parameters.Add(new InventorParameter { Name = "height", Expression = "width * 2", Unit = "mm" });
            return doc;
        }
    }
}
