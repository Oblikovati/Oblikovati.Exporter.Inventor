// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Exercises the REAL adapter recursion + <see cref="ComponentExtractor"/> via faked Inventor
    /// assemblies (the fakes subclass the stub), so the assembly walk runs with no Inventor install.
    /// </summary>
    public sealed class ComponentExtractionTests
    {
        // A part document shared by two occurrences (same FullFileName → one IR document).
        private static _Document Box() => new FakePartDocument(
            "box.ipt", @"C:\work\box.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>());

        [Fact]
        public void Extracts_two_occurrences_with_transforms_and_dedups_the_shared_component()
        {
            _Document box = Box();
            var occurrences = new List<ComponentOccurrence>
            {
                new FakeComponentOccurrence("Box:1", box, new double[] { 0, 0, 0 }),
                new FakeComponentOccurrence("Box:2", box, new double[] { 10, 0, 0 }),
            };
            var asm = new FakeAssemblyDocument("rig.iam", @"C:\work\rig.iam", occurrences);
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(asm));

            InventorDocument ir = adapter.ExtractActiveDocument();

            Assert.Equal(InventorDocumentKind.Assembly, ir.Kind);
            Assert.Equal(2, ir.Occurrences.Count);
            Assert.Equal("Box:2", ir.Occurrences[1].Name);
            Assert.Equal(new double[] { 10, 0, 0 }, ir.Occurrences[1].Position);
            // Both occurrences resolve to the SAME IR component (deduped by full file name).
            Assert.Same(ir.Occurrences[0].Component, ir.Occurrences[1].Component);
        }

        [Fact]
        public void Skips_a_suppressed_occurrence()
        {
            // Regression: a suppressed occurrence raises E_FAIL on Definition/Document and must be
            // skipped, not abort the whole assembly export.
            _Document box = Box();
            var occurrences = new List<ComponentOccurrence>
            {
                new FakeComponentOccurrence("Box:1", box, new double[] { 0, 0, 0 }),
                new FakeComponentOccurrence("Box:2", box, new double[] { 10, 0, 0 }, suppressed: true),
            };
            var asm = new FakeAssemblyDocument("rig.iam", @"C:\work\rig.iam", occurrences);
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(asm));

            InventorDocument ir = adapter.ExtractActiveDocument();

            Assert.Single(ir.Occurrences);
            Assert.Equal("Box:1", ir.Occurrences[0].Name);
        }
    }
}
