// SPDX-License-Identifier: GPL-2.0-only
using System.IO;

namespace Oblikovati.Exporter.Inventor.Entry
{
    /// <summary>
    /// Where exported document files are written. Abstracted so the orchestration is
    /// testable with a fake sink instead of touching the filesystem.
    /// </summary>
    public interface IDocumentSink
    {
        /// <summary>Writes one document file (an assembly's components share the sink's folder).</summary>
        void Write(string fileName, string yaml);
    }

    /// <summary>Writes document files into a directory on disk.</summary>
    public sealed class DirectoryDocumentSink : IDocumentSink
    {
        private readonly string _directory;

        public DirectoryDocumentSink(string directory)
        {
            _directory = directory;
        }

        public void Write(string fileName, string yaml)
        {
            File.WriteAllText(Path.Combine(_directory, fileName), yaml);
        }
    }
}
