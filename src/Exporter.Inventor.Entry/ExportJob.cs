// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;
using Oblikovati.Exporter.Inventor.Translate;

namespace Oblikovati.Exporter.Inventor.Entry
{
    /// <summary>
    /// The whole export, end to end and free of the Inventor API: read the active document,
    /// translate the tree, write every file to <paramref name="sink"/>, and return the user
    /// summary. A part yields one file; an assembly yields its .oad plus its component files.
    /// The add-in supplies a live session and a directory sink; tests supply fakes.
    /// </summary>
    public static class ExportJob
    {
        public static string Run(IInventorSession session, IDocumentSink sink)
        {
            InventorDocument doc = session.ExtractActiveDocument();
            var report = new ExportReport();
            var writer = new RecipeYamlWriter();
            IReadOnlyList<TranslatedDocument> files =
                new DocumentExporter(new DocumentTranslator()).Export(doc, report);

            foreach (TranslatedDocument file in files)
            {
                sink.Write(file.FileName, writer.Write(file.Document));
            }

            return Summarize(files.Count, report);
        }

        private static string Summarize(int fileCount, ExportReport report)
        {
            string files = fileCount == 1 ? "1 file" : $"{fileCount} files";
            if (report.Unsupported.Count == 0)
            {
                return $"Exported {files}.";
            }
            return $"Exported {files} with {report.Unsupported.Count} unsupported item(s).";
        }
    }
}
