// SPDX-License-Identifier: GPL-2.0-only
using System.Linq;
using Oblikovati.Exporter.Inventor.Fixtures;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;
using Oblikovati.Exporter.Inventor.Translate;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    public sealed class SketchTranslatorTests
    {
        private static SketchData TranslateFirstSketch(InventorDocument doc)
        {
            OblikovatiDocument recipe = new DocumentTranslator().Translate(doc, new ExportReport());
            var part = Assert.IsType<PartRecipe>(recipe.Model);
            return Assert.Single(part.Sketches);
        }

        [Fact]
        public void Rectangle_translates_to_distinct_points_lines_and_constraints()
        {
            SketchData sketch = TranslateFirstSketch(InventorSampleParts.RectanglePart());

            // Four lines, each with its own two endpoints -> 8 distinct points (coincidence is
            // expressed by constraints, not shared ids).
            Assert.Equal(4, sketch.Entities.Count(e => e.Kind == "line"));
            Assert.Equal(8, sketch.Points.Count);
            Assert.Equal(4, sketch.Constraints.Count(c => c.Kind == "coincident"));
            Assert.Equal(2, sketch.Constraints.Count(c => c.Kind == "horizontal"));
            Assert.Equal(2, sketch.Constraints.Count(c => c.Kind == "vertical"));
            Assert.Single(sketch.Constraints, c => c.Kind == "fix");
        }

        [Fact]
        public void Rectangle_dimensions_link_to_parameters_in_centimetres()
        {
            SketchData sketch = TranslateFirstSketch(InventorSampleParts.RectanglePart());

            Assert.Equal(2, sketch.Dimensions.Count);
            Assert.Contains(sketch.Dimensions, d => d.Kind == "distance" && d.Expression == "width");
            Assert.Contains(sketch.Dimensions, d => d.Kind == "distance" && d.Expression == "height");
            // Coordinates pass through unscaled: Inventor's cm is the recipe unit (4 cm wide).
            Assert.Contains(sketch.Points, p => p.X == 4 && p.Y == 0);
        }

        [Fact]
        public void Circle_translates_to_center_point_radius_and_diameter_dimension()
        {
            SketchData sketch = TranslateFirstSketch(InventorSampleParts.CirclePart());

            EntityData circle = Assert.Single(sketch.Entities);
            Assert.Equal("circle", circle.Kind);
            Assert.Equal(2.0, circle.Radius);
            Assert.Single(circle.Points); // center only
            Assert.Contains(sketch.Dimensions, d => d.Kind == "diameter" && d.Expression == "dia");
        }
    }
}
