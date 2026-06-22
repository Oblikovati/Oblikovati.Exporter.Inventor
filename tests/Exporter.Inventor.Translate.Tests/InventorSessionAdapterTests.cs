// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Inventor;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    public sealed class InventorSessionAdapterTests
    {
        private static FakePartDocument Part(
            string displayName = "bracket.ipt",
            string path = @"C:\work\bracket.ipt",
            UnitsOfMeasure? units = null,
            IList<Parameter>? userParameters = null) =>
            new FakePartDocument(
                displayName, path, units ?? new FakeUnitsOfMeasure(), userParameters ?? new List<Parameter>());

        [Fact]
        public void Reads_part_kind_and_strips_extension_from_name()
        {
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(Part()));

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
        public void Extracts_length_and_angle_units_as_abbreviations()
        {
            var units = new FakeUnitsOfMeasure(
                UnitsTypeEnum.kInchLengthUnits, UnitsTypeEnum.kRadianAngleUnits);
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(Part(units: units)));

            InventorDocument ir = adapter.ExtractActiveDocument();

            Assert.Equal("in", ir.LengthUnit);
            Assert.Equal("rad", ir.AngleUnit);
        }

        [Fact]
        public void Extracts_user_parameters_with_expression_and_unit()
        {
            var ups = new List<Parameter>
            {
                new FakeParameter("Width", "40 mm", "mm"),
                new FakeParameter("Height", "Width * 2", "mm"),
            };
            var adapter = new InventorSessionAdapter(
                new FakeInventorApplication(Part(userParameters: ups)));

            InventorDocument ir = adapter.ExtractActiveDocument();

            Assert.Equal(2, ir.Parameters.Count);
            Assert.Equal("Width", ir.Parameters[0].Name);
            Assert.Equal("40 mm", ir.Parameters[0].Expression);
            Assert.Equal("mm", ir.Parameters[0].Unit);
            Assert.Equal("Height", ir.Parameters[1].Name);
            Assert.Equal("Width * 2", ir.Parameters[1].Expression);
        }

        [Fact]
        public void ShowMessage_assigns_the_status_bar_text()
        {
            var app = new FakeInventorApplication(Part());
            var adapter = new InventorSessionAdapter(app);

            adapter.ShowMessage("Exported bracket.opd.");

            Assert.Equal("Exported bracket.opd.", app.LastStatus);
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
