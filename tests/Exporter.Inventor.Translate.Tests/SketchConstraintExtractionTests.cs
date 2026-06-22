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
    /// Exercises the REAL SketchExtractor's explicit constraint + dimension extraction via faked
    /// sketch entities. Constraints/dimensions reference the same entity instances the sketch
    /// holds, so the extractor's COM-identity mapping resolves them to the IR curves/points.
    /// </summary>
    public sealed class SketchConstraintExtractionTests
    {
        private static InventorSketch Extract(
            IList<SketchLine> lines, IList<SketchCircle> circles,
            IList<GeometricConstraint> constraints, IList<DimensionConstraint> dimensions)
        {
            var sketch = new FakePlanarSketch(
                "S", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                new FakePlane(new FakeUnitVector(0, 0, 1)), lines, circles, constraints, dimensions);
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                new List<PlanarSketch> { sketch });
            return new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument().Sketches[0];
        }

        [Fact]
        public void Reads_horizontal_and_a_parameter_linked_distance_dimension_on_a_line()
        {
            SketchLine bottom = FakeSketchLine.From(0, 0, 4, 0);
            var lines = new List<SketchLine> { bottom };
            var constraints = new List<GeometricConstraint> { new FakeHorizontalConstraint(bottom) };
            var dims = new List<DimensionConstraint>
            {
                new FakeTwoPointDistanceDimConstraint(bottom.StartSketchPoint, bottom.EndSketchPoint, "width"),
            };

            InventorSketch sk = Extract(lines, new List<SketchCircle>(), constraints, dims);

            Assert.Contains(sk.Constraints, c => c.Kind == InventorConstraintKind.Horizontal && c.Curves.Count == 1);
            InventorSketchDimension dim = Assert.Single(sk.Dimensions);
            Assert.Equal(InventorDimensionKind.Distance, dim.Kind);
            Assert.Equal("width", dim.Expression);
            Assert.Equal(2, dim.Points.Count); // bottom line's two endpoints
        }

        [Fact]
        public void Reads_parallel_constraint_between_two_lines()
        {
            SketchLine bottom = FakeSketchLine.From(0, 0, 4, 0);
            SketchLine top = FakeSketchLine.From(0, 3, 4, 3);
            var constraints = new List<GeometricConstraint> { new FakeParallelConstraint(bottom, top) };

            InventorSketch sk = Extract(
                new List<SketchLine> { bottom, top }, new List<SketchCircle>(),
                constraints, new List<DimensionConstraint>());

            InventorSketchConstraint parallel = Assert.Single(sk.Constraints, c => c.Kind == InventorConstraintKind.Parallel);
            Assert.Equal(2, parallel.Curves.Count);
        }

        [Fact]
        public void Reads_collinear_equal_length_and_an_angle_dimension_between_two_lines()
        {
            SketchLine a = FakeSketchLine.From(0, 0, 2, 0);
            SketchLine b = FakeSketchLine.From(3, 0, 5, 0);
            var constraints = new List<GeometricConstraint>
            {
                new FakeCollinearConstraint(a, b),
                new FakeEqualLengthConstraint(a, b),
            };
            var dims = new List<DimensionConstraint> { new FakeTwoLineAngleDimConstraint(a, b, "ang") };

            InventorSketch sk = Extract(new List<SketchLine> { a, b }, new List<SketchCircle>(), constraints, dims);

            Assert.Single(sk.Constraints, c => c.Kind == InventorConstraintKind.Collinear && c.Curves.Count == 2);
            Assert.Single(sk.Constraints, c => c.Kind == InventorConstraintKind.EqualLength && c.Curves.Count == 2);
            InventorSketchDimension ang = Assert.Single(sk.Dimensions, d => d.Kind == InventorDimensionKind.Angle);
            Assert.Equal("ang", ang.Expression);
            Assert.Equal(2, ang.Curves.Count);
        }

        [Fact]
        public void Reads_concentric_equal_radius_and_tangent_on_circles()
        {
            SketchCircle c1 = new FakeSketchCircle(0, 0, 1);
            SketchCircle c2 = new FakeSketchCircle(0, 0, 1);
            SketchLine line = FakeSketchLine.From(10, 1, 14, 1);
            var constraints = new List<GeometricConstraint>
            {
                new FakeConcentricConstraint(c1, c2),
                new FakeEqualRadiusConstraint(c1, c2),
                new FakeTangentConstraint(line, c2),
            };

            InventorSketch sk = Extract(
                new List<SketchLine> { line }, new List<SketchCircle> { c1, c2 },
                constraints, new List<DimensionConstraint>());

            Assert.Single(sk.Constraints, c => c.Kind == InventorConstraintKind.Concentric);
            Assert.Single(sk.Constraints, c => c.Kind == InventorConstraintKind.EqualRadius);
            Assert.Single(sk.Constraints, c => c.Kind == InventorConstraintKind.Tangent && c.Curves.Count == 2);
        }

        [Fact]
        public void Reads_diameter_dimension_on_a_circle()
        {
            SketchCircle circle = new FakeSketchCircle(0, 0, 2);
            var dims = new List<DimensionConstraint> { new FakeDiameterDimConstraint(circle, "dia") };

            InventorSketch sk = Extract(
                new List<SketchLine>(), new List<SketchCircle> { circle },
                new List<GeometricConstraint>(), dims);

            InventorSketchDimension dim = Assert.Single(sk.Dimensions);
            Assert.Equal(InventorDimensionKind.Diameter, dim.Kind);
            Assert.Equal("dia", dim.Expression);
            Assert.Equal(1, dim.Curves.Single());
        }
    }
}
