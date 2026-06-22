// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;
using Oblikovati.Exporter.Inventor.Entry;
using Oblikovati.Exporter.Inventor.Inv;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Runs the whole pipeline through the REAL adapter (fed a faked Inventor application that
    /// subclasses the stub), so the extraction logic — not just the translator — is exercised
    /// end to end with no Inventor install: Inventor API → IR → recipe → YAML.
    /// </summary>
    public sealed class ExtractionDryRunTests
    {
        [Fact]
        public void Part_with_parameters_extracts_through_to_opd_yaml()
        {
            var units = new FakeUnitsOfMeasure(
                UnitsTypeEnum.kMillimeterLengthUnits, UnitsTypeEnum.kDegreeAngleUnits);
            var ups = new List<UserParameter>
            {
                new FakeUserParameter("Width", "40 mm", "mm"),
                new FakeUserParameter("Height", "Width * 2", "mm"),
            };
            var doc = new FakePartDocument("bracket.ipt", @"C:\work\bracket.ipt", units, ups);
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(doc));
            var sink = new FakeDocumentSink();

            string summary = ExportJob.Run(adapter, sink);

            string yaml = sink.Files["bracket.opd"];
            Assert.Contains("documentType: 1", yaml);
            Assert.Contains("length: mm", yaml);
            Assert.Contains("name: Width", yaml);
            Assert.Contains("expression: 40 mm", yaml);
            Assert.Contains("expression: Width * 2", yaml);
            Assert.Equal("Exported 1 file.", summary);
        }
    }
}
