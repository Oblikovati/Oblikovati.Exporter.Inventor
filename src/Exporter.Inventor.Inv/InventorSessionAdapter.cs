// SPDX-License-Identifier: GPL-2.0-only
// System.IO is aliased: the Inventor namespace defines its own Path and File types, which
// would otherwise collide with System.IO.Path / System.IO.File once `using Inventor;` is in scope.
using System.Collections.Generic;
using IO = System.IO;
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

        public InventorDocument ExtractActiveDocument(ExportReport report) =>
            ExtractDocument(ActiveDocument(), new Dictionary<string, InventorDocument>(), report);

        /// <summary>
        /// Convenience overload for callers (mainly tests) that don't inspect the report; drops
        /// into a throwaway one so the extraction runs the same way.
        /// </summary>
        public InventorDocument ExtractActiveDocument() => ExtractActiveDocument(new ExportReport());

        /// <summary>
        /// Extracts one document (recursing into an assembly's components). <paramref name="cache"/>
        /// dedups by full file name so a component shared by several occurrences yields one IR
        /// document (one exported file); registering before recursing also guards against cycles.
        /// </summary>
        private InventorDocument ExtractDocument(
            _Document doc, IDictionary<string, InventorDocument> cache, ExportReport report)
        {
            string key = doc.FullFileName;
            if (!string.IsNullOrEmpty(key) && cache.TryGetValue(key, out InventorDocument existing))
            {
                return existing;
            }

            var ir = new InventorDocument
            {
                DisplayName = NameWithoutExtension(doc.DisplayName),
                Kind = ToKind(doc.DocumentType),
            };
            if (!string.IsNullOrEmpty(key))
            {
                cache[key] = ir;
            }

            ExtractUnits(doc, ir);
            if (ir.Kind == InventorDocumentKind.Part)
            {
                ExtractPart((PartDocument)doc, ir, report);
            }
            else
            {
                ComponentExtractor.Extract(
                    ((AssemblyDocument)doc).ComponentDefinition, ir,
                    child => ExtractDocument(child, cache, report));
            }

            return ir;
        }

        private static void ExtractPart(PartDocument part, InventorDocument ir, ExportReport report)
        {
            ExtractUserParameters(part, ir);
            SketchExtractor.Extract(part, ir, report);
            FeatureExtractor.Extract(part, ir);
        }

        /// <summary>
        /// Copies the document's length/angle units into the IR as the abbreviations the
        /// Oblikovati recipe stores (e.g. "mm", "deg").
        /// </summary>
        private static void ExtractUnits(_Document doc, InventorDocument ir)
        {
            UnitsOfMeasure uom = doc.UnitsOfMeasure;
            ir.LengthUnit = LengthAbbreviation(uom.LengthUnits);
            ir.AngleUnit = AngleAbbreviation(uom.AngleUnits);
        }

        // Inventor's GetStringFromType returns full unit *names* ("centimeter", "degree"), but the
        // Oblikovati reader only registers abbreviations (cm, mm, m, in, ft, rad, deg) and rejects
        // anything else — so a live-exported part fails to load. Map from the stable UnitsTypeEnum
        // instead, which is locale-independent and never a display string. Values in the recipe are
        // always stored in database units (cm / radian), so the unit token is a display preference
        // only; an unrecognised Inventor unit falls back to the database unit, keeping geometry exact.
        private static string LengthAbbreviation(UnitsTypeEnum units)
        {
            switch (units)
            {
                case UnitsTypeEnum.kMillimeterLengthUnits: return "mm";
                case UnitsTypeEnum.kCentimeterLengthUnits: return "cm";
                case UnitsTypeEnum.kMeterLengthUnits: return "m";
                case UnitsTypeEnum.kInchLengthUnits: return "in";
                case UnitsTypeEnum.kFootLengthUnits: return "ft";
                default: return "cm";
            }
        }

        private static string AngleAbbreviation(UnitsTypeEnum units)
        {
            switch (units)
            {
                case UnitsTypeEnum.kRadianAngleUnits: return "rad";
                case UnitsTypeEnum.kDegreeAngleUnits: return "deg";
                default: return "deg";
            }
        }

        /// <summary>
        /// Copies the user-authored named parameters (with their expressions and units) into the
        /// IR. Model parameters (d0, d1…) are feature/sketch dimensions captured with their owning
        /// feature later, not here.
        /// </summary>
        private static void ExtractUserParameters(PartDocument doc, InventorDocument ir)
        {
            UserParameters parameters = doc.ComponentDefinition.Parameters.UserParameters;
            // Inventor collections are 1-based. UserParameter exposes Name/Expression directly;
            // Units has asymmetric COM accessors (get→string, set→object) so it imports as
            // get_Units() rather than a property.
            for (int i = 1; i <= parameters.Count; i++)
            {
                UserParameter p = parameters[i];
                ir.Parameters.Add(new InventorParameter
                {
                    Name = p.Name,
                    Expression = InventorExpression.Canonical(p.Expression),
                    Unit = p.get_Units(),
                });
            }
        }

        public string OutputDirectory()
        {
            string? dir = IO.Path.GetDirectoryName(ActiveDocument().FullFileName);
            return string.IsNullOrEmpty(dir) ? IO.Directory.GetCurrentDirectory() : dir!;
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
            IO.Path.GetFileNameWithoutExtension(displayName);
    }
}
