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
            IList<UserParameter>? userParameters = null) =>
            new FakePartDocument(
                displayName, path, units ?? new FakeUnitsOfMeasure(), userParameters ?? new List<UserParameter>());

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
            var doc = new FakeAssemblyDocument("rig.iam", @"C:\work\rig.iam");
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
        public void Maps_full_inventor_unit_names_to_reader_abbreviations()
        {
            // Regression: a live part reports centimeter/degree; the Oblikovati reader only
            // registers abbreviations and rejects "centimeter", so the document failed to load.
            var units = new FakeUnitsOfMeasure(
                UnitsTypeEnum.kCentimeterLengthUnits, UnitsTypeEnum.kDegreeAngleUnits);
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(Part(units: units)));

            InventorDocument ir = adapter.ExtractActiveDocument();

            Assert.Equal("cm", ir.LengthUnit);
            Assert.Equal("deg", ir.AngleUnit);
        }

        [Fact]
        public void Extracts_user_parameters_with_expression_and_unit()
        {
            var ups = new List<UserParameter>
            {
                new FakeUserParameter("Width", "40 mm", "mm"),
                new FakeUserParameter("Height", "Width * 2", "mm"),
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
        public void Normalises_comma_locale_decimal_separator_in_expressions()
        {
            // Regression: on a comma-decimal Windows host, Inventor returns "12,5 mm"; the
            // Oblikovati parser only accepts '.' and rejects the comma. The exporter must
            // canonicalise regardless of the host's regional settings.
            var original = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.GetCultureInfo("de-DE");
                var ups = new List<UserParameter> { new FakeUserParameter("Thickness", "12,5 mm", "mm") };
                var adapter = new InventorSessionAdapter(
                    new FakeInventorApplication(Part(userParameters: ups)));

                InventorDocument ir = adapter.ExtractActiveDocument();

                Assert.Equal("12.5 mm", ir.Parameters[0].Expression);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Fact]
        public void Strips_inventor_unitless_ul_token_from_expressions()
        {
            // Regression: Inventor spells its UNITLESS unit "ul" and emits it on a dimensionless
            // literal, e.g. "15 mm / 2 ul". The Oblikovati parser does not register "ul" and rejects
            // the dangling token with "unexpected \"ul\"", so the exported .opd failed to load
            // (Reel10Inches.ipt et al). The canonical form drops the token: "15 mm / 2 ul" → "15 mm / 2".
            var ups = new List<UserParameter>
            {
                new FakeUserParameter("HalfWidth", "15 mm / 2 ul", "mm"),
                new FakeUserParameter("Ratio", "3 ul", "ul"),
                // "ul" is only stripped as a whole token — a longer name that merely starts with it
                // must survive untouched.
                new FakeUserParameter("Scaled", "ul_count * 2 ul", "mm"),
            };
            var adapter = new InventorSessionAdapter(
                new FakeInventorApplication(Part(userParameters: ups)));

            InventorDocument ir = adapter.ExtractActiveDocument();

            Assert.Equal("15 mm / 2", ir.Parameters[0].Expression);
            Assert.Equal("3", ir.Parameters[1].Expression);
            Assert.Equal("ul_count * 2", ir.Parameters[2].Expression);
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
