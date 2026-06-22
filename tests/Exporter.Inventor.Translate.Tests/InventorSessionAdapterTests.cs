// SPDX-License-Identifier: GPL-2.0-only
using System;
using Inventor;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    public sealed class InventorSessionAdapterTests
    {
        [Fact]
        public void Reads_part_kind_and_strips_extension_from_name()
        {
            var doc = new FakeInventorDocument(
                DocumentTypeEnum.kPartDocumentObject, "bracket.ipt", @"C:\work\bracket.ipt");
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(doc));

            InventorDocument ir = adapter.ExtractActiveDocument();

            Assert.Equal("bracket", ir.DisplayName);
            Assert.Equal(InventorDocumentKind.Part, ir.Kind);
        }

        [Fact]
        public void Maps_assembly_document_type()
        {
            var doc = new FakeInventorDocument(
                DocumentTypeEnum.kAssemblyDocumentObject, "rig.iam", @"C:\work\rig.iam");
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(doc));

            Assert.Equal(InventorDocumentKind.Assembly, adapter.ExtractActiveDocument().Kind);
        }

        [Fact]
        public void Throws_when_no_document_is_open()
        {
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(null));

            Assert.Throws<InvalidOperationException>(() => adapter.ExtractActiveDocument());
        }

        [Fact]
        public void Rejects_unsupported_document_type()
        {
            var doc = new FakeInventorDocument(
                DocumentTypeEnum.kDrawingDocumentObject, "sheet.idw", @"C:\work\sheet.idw");
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(doc));

            Assert.Throws<NotSupportedException>(() => adapter.ExtractActiveDocument());
        }
    }
}
