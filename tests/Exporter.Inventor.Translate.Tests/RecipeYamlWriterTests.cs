// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Recipe;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    public sealed class RecipeYamlWriterTests
    {
        [Fact]
        public void Writes_envelope_keys_and_omits_nulls()
        {
            var doc = new OblikovatiDocument
            {
                DocumentType = 1,
                DisplayName = "bracket",
                Model = new PartRecipe(),
            };

            string yaml = new RecipeYamlWriter().Write(doc);

            Assert.Contains("schemaVersion: 2", yaml);
            Assert.Contains("documentType: 1", yaml);
            Assert.Contains("displayName: bracket", yaml);
            Assert.Contains("length: mm", yaml);
            // Empty parameter list is omitted, not emitted as "parameters: []".
            Assert.DoesNotContain("parameters:", yaml);
        }
    }
}
