// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Entry;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    public sealed class ExportJobTests
    {
        [Fact]
        public void Part_writes_one_opd_file_and_summary()
        {
            var session = new FakeInventorSession(
                new InventorDocument { DisplayName = "bracket", Kind = InventorDocumentKind.Part });
            var sink = new FakeDocumentSink();

            string summary = ExportJob.Run(session, sink);

            Assert.True(sink.Files.ContainsKey("bracket.opd"));
            Assert.Contains("schemaVersion: 2", sink.Files["bracket.opd"]);
            Assert.Equal("Exported bracket.opd.", summary);
        }

        [Fact]
        public void Assembly_writes_oad_extension()
        {
            var session = new FakeInventorSession(
                new InventorDocument { DisplayName = "rig", Kind = InventorDocumentKind.Assembly });
            var sink = new FakeDocumentSink();

            ExportJob.Run(session, sink);

            Assert.True(sink.Files.ContainsKey("rig.oad"));
        }
    }
}
