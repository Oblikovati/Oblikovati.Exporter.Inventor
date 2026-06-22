// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>One translated document and the file name it should be written to.</summary>
    public sealed class TranslatedDocument
    {
        public TranslatedDocument(string fileName, OblikovatiDocument document)
        {
            FileName = fileName;
            Document = document;
        }

        public string FileName { get; }

        public OblikovatiDocument Document { get; }
    }

    /// <summary>
    /// Walks a document tree and produces one <see cref="TranslatedDocument"/> per document: a
    /// part becomes one <c>.opd</c>; an assembly becomes one <c>.oad</c> plus the component
    /// documents it references (recursively). Components shared by several occurrences are
    /// exported once (deduped by reference), and the assembly's occurrences reference them by
    /// owner-relative file name, matching Oblikovati's reference graph.
    /// </summary>
    public sealed class DocumentExporter
    {
        private readonly DocumentTranslator _translator;

        public DocumentExporter(DocumentTranslator translator)
        {
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
        }

        public IReadOnlyList<TranslatedDocument> Export(InventorDocument root, ExportReport report)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (report == null) throw new ArgumentNullException(nameof(report));

            var files = new List<TranslatedDocument>();
            var fileNames = new Dictionary<InventorDocument, string>(ReferenceComparer.Instance);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Emit(root, files, fileNames, usedNames, report);
            return files;
        }

        // Returns the file name the document was (or will be) written to, emitting it once.
        private string Emit(
            InventorDocument doc,
            List<TranslatedDocument> files,
            Dictionary<InventorDocument, string> fileNames,
            HashSet<string> usedNames,
            ExportReport report)
        {
            if (fileNames.TryGetValue(doc, out string? existing))
            {
                return existing;
            }

            string fileName = UniqueName(doc, usedNames);
            fileNames[doc] = fileName;

            if (doc.Kind == InventorDocumentKind.Assembly)
            {
                var occurrences = new List<OccurrenceData>();
                foreach (InventorOccurrence occurrence in doc.Occurrences)
                {
                    string childFile = Emit(occurrence.Component, files, fileNames, usedNames, report);
                    occurrences.Add(OccurrenceTranslator.Translate(occurrence, childFile));
                }

                files.Add(new TranslatedDocument(fileName, _translator.TranslateAssembly(doc, occurrences)));
            }
            else
            {
                files.Add(new TranslatedDocument(fileName, _translator.Translate(doc, report)));
            }

            return fileName;
        }

        private static string UniqueName(InventorDocument doc, HashSet<string> used)
        {
            string ext = doc.Kind == InventorDocumentKind.Assembly ? ".oad" : ".opd";
            string baseName = doc.DisplayName.Length == 0 ? "untitled" : doc.DisplayName;
            string candidate = baseName + ext;
            int n = 2;
            while (!used.Add(candidate))
            {
                candidate = baseName + "_" + n++ + ext;
            }

            return candidate;
        }

        // Identity comparer (netstandard2.0 lacks ReferenceEqualityComparer): two component
        // references are the same exported file only when they are the same object.
        private sealed class ReferenceComparer : IEqualityComparer<InventorDocument>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(InventorDocument? x, InventorDocument? y) => ReferenceEquals(x, y);

            public int GetHashCode(InventorDocument obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
