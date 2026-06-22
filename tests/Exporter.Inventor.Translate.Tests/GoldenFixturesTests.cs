// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Fixtures;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;
using Oblikovati.Exporter.Inventor.Translate;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Asserts the shared golden fixtures in-proc, so the exact documents the round-trip opens
    /// with oblikovati-cli are also unit-checked here. Keeps the emitter honest without needing
    /// the Go reader on every run.
    /// </summary>
    public sealed class GoldenFixturesTests
    {
        private static string Emit(InventorDocument fixture) =>
            new RecipeYamlWriter().Write(new DocumentTranslator().Translate(fixture, new ExportReport()));

        [Fact]
        public void Empty_part_emits_minimal_valid_envelope()
        {
            string yaml = Emit(InventorSampleParts.EmptyPart());

            Assert.Contains("schemaVersion: 2", yaml);
            Assert.Contains("documentType: 1", yaml);
            Assert.Contains("displayName: empty", yaml);
            Assert.DoesNotContain("parameters:", yaml);
        }

        [Fact]
        public void Parametric_part_emits_parameters_with_formula_reference()
        {
            string yaml = Emit(InventorSampleParts.ParametricPart());

            Assert.Contains("name: width", yaml);
            Assert.Contains("expression: 40 mm", yaml);
            Assert.Contains("name: height", yaml);
            Assert.Contains("expression: width * 2", yaml);
        }

        [Fact]
        public void Rectangle_part_emits_a_sketch_with_entities_and_constraints()
        {
            string yaml = Emit(InventorSampleParts.RectanglePart());

            Assert.Contains("sketches:", yaml);
            Assert.Contains("kind: line", yaml);
            Assert.Contains("kind: coincident", yaml);
            Assert.Contains("kind: distance", yaml);
        }

        [Fact]
        public void Circle_part_emits_a_circle_with_a_diameter_dimension()
        {
            string yaml = Emit(InventorSampleParts.CirclePart());

            Assert.Contains("kind: circle", yaml);
            Assert.Contains("kind: diameter", yaml);
        }

        [Fact]
        public void Box_part_emits_an_extrude_feature_over_its_sketch()
        {
            string yaml = Emit(InventorSampleParts.BoxPart());

            Assert.Contains("features:", yaml);
            Assert.Contains("kind: extrude", yaml);
            Assert.Contains("distance: 5", yaml);
        }

        [Fact]
        public void Datum_plane_part_emits_a_fixed_frame_work_feature()
        {
            string yaml = Emit(InventorSampleParts.DatumPlanePart());

            Assert.Contains("workFeatures:", yaml);
            Assert.Contains("kind: fixed-frame", yaml);
        }
    }
}
