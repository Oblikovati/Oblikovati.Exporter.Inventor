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

        private static PartRecipe Translate(Oblikovati.Exporter.Inventor.Model.InventorDocument doc)
        {
            OblikovatiDocument recipe = new DocumentTranslator().Translate(doc, new ExportReport());
            return Assert.IsType<PartRecipe>(recipe.Model);
        }

        [Fact]
        public void Revolve_emits_own_centerline_full_revolution_and_a_centerline_entity()
        {
            PartRecipe part = Translate(InventorSampleParts.RevolvePart());

            FeatureData feature = Assert.Single(part.Features);
            RevolveData revolve = Assert.IsType<RevolveData>(feature.Revolve!);
            Assert.Equal(0, revolve.Sketch);
            Assert.Null(revolve.Angle); // full revolution
            // The axis line is flagged as a centerline so the revolve finds its own axis.
            Assert.Contains(part.Sketches[0].Entities, e => e.Kind == "line" && e.Centerline == true);
        }

        [Fact]
        public void Rectangular_pattern_remaps_its_source_to_the_recipe_extrude_index()
        {
            PartRecipe part = Translate(InventorSampleParts.RectPatternPart());

            FeatureData pat = Assert.Single(part.Features, f => f.Kind == "rectangular-pattern");
            RectPatternData data = Assert.IsType<RectPatternData>(pat.RectangularPattern!);
            Assert.Equal(3, data.CountX);
            Assert.Equal(new double[] { 6, 0, 0 }, data.StepX);
            Assert.Equal(0, Assert.Single(data.Source)); // the extrude is recipe feature 0
        }

        [Fact]
        public void Circular_pattern_full_revolution_becomes_two_pi()
        {
            PartRecipe part = Translate(InventorSampleParts.CircularPatternPart());

            CircPatternData data = Assert.IsType<CircPatternData>(
                Assert.Single(part.Features, f => f.Kind == "circular-pattern").CircularPattern!);
            Assert.Equal(4, data.Count);
            Assert.Equal(2 * System.Math.PI, data.Angle, 6);
        }

        [Fact]
        public void Mirror_emits_plane_origin_and_normal_with_remapped_source()
        {
            PartRecipe part = Translate(InventorSampleParts.MirrorPart());

            MirrorData data = Assert.IsType<MirrorData>(
                Assert.Single(part.Features, f => f.Kind == "mirror").Mirror!);
            Assert.Equal(new double[] { 1, 0, 0 }, data.Normal);
            Assert.Equal(0, Assert.Single(data.Source));
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
