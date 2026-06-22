// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;
using Oblikovati.Exporter.Inventor.Translate;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    public sealed class DocumentTranslatorTests
    {
        [Fact]
        public void Part_translates_to_document_type_1_with_units_and_parameters()
        {
            var source = new InventorDocument
            {
                DisplayName = "bracket",
                Kind = InventorDocumentKind.Part,
                LengthUnit = "mm",
                AngleUnit = "deg",
            };
            source.Parameters.Add(new InventorParameter { Name = "width", Expression = "40 mm" });

            OblikovatiDocument recipe = new DocumentTranslator().Translate(source, new ExportReport());

            Assert.Equal(1, recipe.DocumentType);
            Assert.Equal("bracket", recipe.DisplayName);
            var part = Assert.IsType<PartRecipe>(recipe.Model);
            Assert.Equal("mm", part.Units.Length);
            ParameterRecipe param = Assert.Single(part.Parameters);
            Assert.Equal("width", param.Name);
            Assert.Equal("40 mm", param.Expression);
            Assert.Equal("model", param.Kind);
        }

        [Fact]
        public void Assembly_translates_to_document_type_2()
        {
            var source = new InventorDocument { DisplayName = "rig", Kind = InventorDocumentKind.Assembly };

            OblikovatiDocument recipe = new DocumentTranslator().Translate(source, new ExportReport());

            Assert.Equal(2, recipe.DocumentType);
        }
    }
}
