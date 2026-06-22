// SPDX-License-Identifier: GPL-2.0-only
using Inventor;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Named fake of the Inventor <see cref="Application"/> built by subclassing the all-virtual
    /// stub, so the real <c>InventorSessionAdapter</c> can be exercised with no Inventor install.
    /// </summary>
    public sealed class FakeInventorApplication : Application
    {
        private readonly _Document? _active;

        public FakeInventorApplication(_Document? active)
        {
            _active = active;
        }

        public override _Document? ActiveDocument => _active;

        public string? LastStatus { get; private set; }

        public override void StatusBarText(string text) => LastStatus = text;
    }

    /// <summary>Named fake Inventor document with a fixed kind, name, and path.</summary>
    public sealed class FakeInventorDocument : _Document
    {
        private readonly DocumentTypeEnum _type;
        private readonly string _displayName;
        private readonly string _fullFileName;

        public FakeInventorDocument(DocumentTypeEnum type, string displayName, string fullFileName)
        {
            _type = type;
            _displayName = displayName;
            _fullFileName = fullFileName;
        }

        public override DocumentTypeEnum DocumentType => _type;

        public override string DisplayName => _displayName;

        public override string FullFileName => _fullFileName;
    }
}
