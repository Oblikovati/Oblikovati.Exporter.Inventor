// SPDX-License-Identifier: GPL-2.0-only
using System.IO;
using Inventor;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Inv
{
    /// <summary>
    /// Production <see cref="IInventorSession"/> over a live Inventor <see cref="Application"/>.
    /// Wraps the COM API so nothing above this layer references Inventor types. The rich
    /// extraction (parameters, sketches, features, occurrences) lands in later milestones;
    /// this M0 skeleton resolves the active document's kind, name, and output location.
    /// </summary>
    public sealed class InventorSessionAdapter : IInventorSession
    {
        private readonly Application _application;

        public InventorSessionAdapter(Application application)
        {
            _application = application;
        }

        public InventorDocument ExtractActiveDocument()
        {
            _Document doc = ActiveDocument();
            var ir = new InventorDocument
            {
                DisplayName = NameWithoutExtension(doc.DisplayName),
                Kind = ToKind(doc.DocumentType),
            };
            ExtractUnits(doc, ir);
            if (ir.Kind == InventorDocumentKind.Part)
            {
                ExtractUserParameters((PartDocument)doc, ir);
            }
            return ir;
        }

        /// <summary>
        /// Copies the document's length/angle units into the IR as expression abbreviations
        /// (e.g. "mm", "deg") — the form the Oblikovati recipe stores.
        /// </summary>
        private static void ExtractUnits(_Document doc, InventorDocument ir)
        {
            UnitsOfMeasure uom = doc.UnitsOfMeasure;
            ir.LengthUnit = uom.GetStringFromType(uom.LengthUnits);
            ir.AngleUnit = uom.GetStringFromType(uom.AngleUnits);
        }

        /// <summary>
        /// Copies the user-authored named parameters (with their expressions and units) into the
        /// IR. Model parameters (d0, d1…) are feature/sketch dimensions captured with their owning
        /// feature later, not here.
        /// </summary>
        private static void ExtractUserParameters(PartDocument doc, InventorDocument ir)
        {
            UserParameters parameters = doc.ComponentDefinition.Parameters.UserParameters;
            for (int i = 1; i <= parameters.Count; i++)
            {
                Parameter p = parameters[i];
                ir.Parameters.Add(new InventorParameter
                {
                    Name = p.Name,
                    Expression = p.Expression,
                    Unit = p.Units,
                });
            }
        }

        public string OutputDirectory()
        {
            string? dir = Path.GetDirectoryName(ActiveDocument().FullFileName);
            return string.IsNullOrEmpty(dir) ? Directory.GetCurrentDirectory() : dir!;
        }

        public void ShowMessage(string message) => _application.StatusBarText = message;

        private _Document ActiveDocument()
        {
            _Document? doc = _application.ActiveDocument;
            if (doc == null)
            {
                throw new System.InvalidOperationException(
                    "No active Inventor document to export; open a part or assembly first.");
            }
            return doc;
        }

        private static InventorDocumentKind ToKind(DocumentTypeEnum type)
        {
            if (type == DocumentTypeEnum.kAssemblyDocumentObject)
            {
                return InventorDocumentKind.Assembly;
            }
            if (type == DocumentTypeEnum.kPartDocumentObject)
            {
                return InventorDocumentKind.Part;
            }
            throw new System.NotSupportedException(
                $"Unsupported document type '{type}'; only part and assembly documents export.");
        }

        private static string NameWithoutExtension(string displayName) =>
            Path.GetFileNameWithoutExtension(displayName);
    }
}
