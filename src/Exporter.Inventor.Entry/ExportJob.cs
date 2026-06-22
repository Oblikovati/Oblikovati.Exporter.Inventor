// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;
using Oblikovati.Exporter.Inventor.Translate;

namespace Oblikovati.Exporter.Inventor.Entry
{
    /// <summary>
    /// The whole export, end to end and free of the Inventor API: read the active document,
    /// translate it, write the file to <paramref name="sink"/>, and return the user summary.
    /// The add-in supplies a live session and a directory sink; tests supply fakes.
    /// </summary>
    public static class ExportJob
    {
        public static string Run(IInventorSession session, IDocumentSink sink)
        {
            InventorDocument doc = session.ExtractActiveDocument();
            var report = new ExportReport();
            OblikovatiDocument recipe = new DocumentTranslator().Translate(doc, report);

            string fileName = doc.DisplayName + Extension(doc.Kind);
            sink.Write(fileName, new RecipeYamlWriter().Write(recipe));

            return Summarize(fileName, report);
        }

        /// <summary>.opd for a part, .oad for an assembly (matches Oblikovati's extensions).</summary>
        private static string Extension(InventorDocumentKind kind) =>
            kind == InventorDocumentKind.Assembly ? ".oad" : ".opd";

        private static string Summarize(string fileName, ExportReport report)
        {
            if (report.Unsupported.Count == 0)
            {
                return $"Exported {fileName}.";
            }
            return $"Exported {fileName} with {report.Unsupported.Count} unsupported item(s).";
        }
    }
}
