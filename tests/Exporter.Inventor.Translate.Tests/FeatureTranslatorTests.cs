// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Fixtures;
using Oblikovati.Exporter.Inventor.Recipe;
using Oblikovati.Exporter.Inventor.Translate;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    public sealed class FeatureTranslatorTests
    {
        private static PartRecipe TranslateBox()
        {
            OblikovatiDocument recipe = new DocumentTranslator().Translate(InventorSampleParts.BoxPart(), new ExportReport());
            return Assert.IsType<PartRecipe>(recipe.Model);
        }

        [Fact]
        public void Box_emits_an_extrude_over_the_first_sketch_in_centimetres()
        {
            PartRecipe part = TranslateBox();

            FeatureData feature = Assert.Single(part.Features);
            Assert.Equal("extrude", feature.Kind);
            ExtrudeData extrude = Assert.IsType<ExtrudeData>(feature.Extrude!);
            Assert.Equal(0, extrude.Sketch);
            Assert.Equal(0, Assert.Single(extrude.Profiles));
            Assert.Equal("newBody", extrude.Operation);
            Assert.Equal("distance", extrude.Extent);
            Assert.Equal(5.0, extrude.Distance);
        }

        [Fact]
        public void Box_keeps_its_sketch_so_the_extrude_can_reference_it()
        {
            PartRecipe part = TranslateBox();

            Assert.Single(part.Sketches);
        }
    }

    public sealed class WorkPlaneTranslatorTests
    {
        [Fact]
        public void Datum_translates_to_a_fixed_frame_work_feature_at_its_origin()
        {
            OblikovatiDocument recipe =
                new DocumentTranslator().Translate(InventorSampleParts.DatumPlanePart(), new ExportReport());
            var part = Assert.IsType<PartRecipe>(recipe.Model);

            WorkFeatureData work = Assert.Single(part.WorkFeatures);
            Assert.Equal("plane", work.Collection);
            Assert.Equal("fixed-frame", work.Kind);
            Assert.Equal(new double[] { 0, 0, 5 }, work.Position);
        }
    }
}
