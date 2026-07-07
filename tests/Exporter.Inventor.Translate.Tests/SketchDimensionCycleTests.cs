// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using System.Linq;
using Inventor;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Regression for the dependency-cycle bug that blocked MainFrame.ipt / HeadShield.ipt from
    /// loading in the Oblikovati reader ("expression for \"d1\" (id 2) forms a dependency cycle").
    ///
    /// Inventor backs every sketch dimension with an auto-named model parameter (d0, d1, …), and a
    /// dimension can be linked to another by an expression naming it (Inventor's d2 = "d1"). The
    /// exporter drops those dN names (the reader re-mints its own per sketch as it restores
    /// dimensions in order), so a transcribed "d1" collided with the reader's freshly-minted "d1"
    /// and became a self-reference. The exporter now collapses any dimension expression that
    /// references a foreign (non-user) model parameter to its evaluated value; literals and
    /// user-parameter references still pass through.
    /// </summary>
    public sealed class SketchDimensionCycleTests
    {
        private static InventorSketch Extract(
            IList<UserParameter> userParameters, IList<DimensionConstraint> dimensions, IList<SketchLine> lines)
        {
            var sketch = new FakePlanarSketch(
                "S", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                new FakePlane(new FakeUnitVector(0, 0, 1)), lines, new List<SketchCircle>(),
                new List<GeometricConstraint>(), dimensions);
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), userParameters,
                new List<PlanarSketch> { sketch });
            InventorDocument ir = new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument();
            return ir.Sketches[0];
        }

        [Fact]
        public void Collapses_a_dimension_that_references_an_inventor_model_parameter_to_its_value()
        {
            // Two lines, two distance dimensions mirroring MainFrame's Sketch1: the first is a
            // literal, the second is Inventor's "d1" (a link to the first dimension's model param).
            SketchLine a = FakeSketchLine.From(0, 0, 26.67, 0);
            SketchLine b = FakeSketchLine.From(0, 5, 26.67, 5);
            var dims = new List<DimensionConstraint>
            {
                new FakeTwoPointDistanceDimConstraint(a.StartSketchPoint, a.EndSketchPoint, "10.5 in", 26.67),
                new FakeTwoPointDistanceDimConstraint(b.StartSketchPoint, b.EndSketchPoint, "d1", 26.67),
            };

            InventorSketch sk = Extract(
                new List<UserParameter>(), dims, new List<SketchLine> { a, b });

            Assert.Equal(2, sk.Dimensions.Count);
            Assert.Equal("10.5 in", sk.Dimensions[0].Expression);   // literal preserved
            // The "d1" reference cannot round-trip, so it is emitted as the model value in cm — no
            // dangling dN reference for the reader to re-mint into a self-cycle.
            Assert.Equal("26.67 cm", sk.Dimensions[1].Expression);
            Assert.DoesNotContain(sk.Dimensions, d => d.Expression.Contains("d1"));
        }

        [Fact]
        public void Preserves_a_dimension_expression_that_references_only_user_parameters()
        {
            // "width" is an authored user parameter (emitted by name), so a reference to it binds
            // in the reader and must be kept verbatim rather than flattened to a value.
            SketchLine a = FakeSketchLine.From(0, 0, 8, 0);
            var dims = new List<DimensionConstraint>
            {
                new FakeTwoPointDistanceDimConstraint(a.StartSketchPoint, a.EndSketchPoint, "width * 2", 8.0),
            };

            InventorSketch sk = Extract(
                new List<UserParameter> { new FakeUserParameter("width", "40 mm", "mm") },
                dims, new List<SketchLine> { a });

            Assert.Equal("width * 2", Assert.Single(sk.Dimensions).Expression);
        }

        [Fact]
        public void Collapses_only_the_foreign_model_reference_in_a_mixed_expression()
        {
            // A user param named exactly like a model param (d5) is still emitted, so a reference to
            // it is safe; but a genuine foreign model reference (d7) forces the collapse.
            SketchLine a = FakeSketchLine.From(0, 0, 4, 0);
            SketchLine b = FakeSketchLine.From(0, 2, 4, 2);
            var dims = new List<DimensionConstraint>
            {
                new FakeTwoPointDistanceDimConstraint(a.StartSketchPoint, a.EndSketchPoint, "d5 + 1 cm", 4.0),
                new FakeTwoPointDistanceDimConstraint(b.StartSketchPoint, b.EndSketchPoint, "d7", 2.0),
            };

            InventorSketch sk = Extract(
                new List<UserParameter> { new FakeUserParameter("d5", "30 mm", "mm") },
                dims, new List<SketchLine> { a, b });

            Assert.Equal("d5 + 1 cm", sk.Dimensions[0].Expression); // d5 is a user param -> kept
            Assert.Equal("2 cm", sk.Dimensions[1].Expression);      // d7 is foreign -> collapsed
        }
    }
}
