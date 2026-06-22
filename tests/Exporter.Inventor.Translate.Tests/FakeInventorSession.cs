// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Entry;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Named fake of <see cref="IInventorSession"/> (CLAUDE.md: fakes, not inline stubs) that
    /// returns a canned document so the orchestration runs with no Inventor install.
    /// </summary>
    public sealed class FakeInventorSession : IInventorSession
    {
        private readonly InventorDocument _document;

        public FakeInventorSession(InventorDocument document)
        {
            _document = document;
        }

        public InventorDocument ExtractActiveDocument() => _document;

        public string OutputDirectory() => ".";

        public string? LastMessage { get; private set; }

        public void ShowMessage(string message) => LastMessage = message;
    }

    /// <summary>In-memory <see cref="IDocumentSink"/> capturing written files for assertions.</summary>
    public sealed class FakeDocumentSink : IDocumentSink
    {
        public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();

        public void Write(string fileName, string yaml) => Files[fileName] = yaml;
    }
}
