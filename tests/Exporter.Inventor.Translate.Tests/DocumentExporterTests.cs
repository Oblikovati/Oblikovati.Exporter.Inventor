// SPDX-License-Identifier: GPL-2.0-only
using System.Linq;
using Oblikovati.Exporter.Inventor.Fixtures;
using Oblikovati.Exporter.Inventor.Translate;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    public sealed class DocumentExporterTests
    {
        [Fact]
        public void Assembly_emits_one_oad_plus_one_deduped_component_opd()
        {
            var files = new DocumentExporter(new DocumentTranslator())
                .Export(InventorSampleParts.AssemblyDoc(), new ExportReport());

            // Two box occurrences share one component -> assembly.oad + box.opd (deduped to one).
            Assert.Equal(2, files.Count);
            TranslatedDocument oad = Assert.Single(files, f => f.FileName.EndsWith(".oad"));
            Assert.Equal("assembly.oad", oad.FileName);
            Assert.Single(files, f => f.FileName == "box.opd");

            string oadYaml = new Oblikovati.Exporter.Inventor.Recipe.RecipeYamlWriter().Write(oad.Document);
            Assert.Contains("documentType: 2", oadYaml);
            Assert.Contains("component: box.opd", oadYaml);
        }
    }
}
