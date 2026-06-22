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
    /// Exercises the REAL FeatureExtractor's pattern/mirror walk via faked Inventor features.
    /// A single extrude named "Extrude1" stands in as the patterned source; patterns reference
    /// it by name (as the resolver does against a real part).
    /// </summary>
    public sealed class PatternExtractionTests
    {
        private static InventorDocument Extract(
            IList<RectangularPatternFeature>? rect = null,
            IList<CircularPatternFeature>? circ = null,
            IList<MirrorFeature>? mirror = null)
        {
            var sketches = new List<PlanarSketch> { FakePlanarSketch.Square() };
            var extrudes = new List<ExtrudeFeature>
            {
                new FakeExtrudeFeature("Extrude1", PartFeatureOperationEnum.kNewBodyOperation, "Square", 5),
            };
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                sketches, extrudes, null, null, rect, circ, mirror);
            return new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument();
        }

        [Fact]
        public void Rectangular_pattern_reads_count_step_and_source_from_a_work_axis()
        {
            var xAxis = new FakeWorkAxis(new double[] { 0, 0, 0 }, new double[] { 1, 0, 0 });
            var rect = new List<RectangularPatternFeature>
            {
                new FakeRectangularPatternFeature("Pattern1", new[] { "Extrude1" }, 3, 6, xAxis),
            };

            InventorDocument ir = Extract(rect: rect);

            var p = Assert.IsType<InventorRectangularPattern>(ir.Features.Last());
            Assert.Equal(3, p.CountX);
            Assert.Equal(new double[] { 6, 0, 0 }, p.StepX); // unit X * 6 cm spacing
            Assert.Equal(0, Assert.Single(p.SourceFeatureIndices)); // the extrude is IR feature 0
        }

        [Fact]
        public void Circular_pattern_reads_axis_point_direction_count_and_angle()
        {
            var zAxis = new FakeWorkAxis(new double[] { 0, 0, 0 }, new double[] { 0, 0, 1 });
            var circ = new List<CircularPatternFeature>
            {
                new FakeCircularPatternFeature("Pattern1", new[] { "Extrude1" }, 4, 2 * System.Math.PI, zAxis),
            };

            InventorDocument ir = Extract(circ: circ);

            var p = Assert.IsType<InventorCircularPattern>(ir.Features.Last());
            Assert.Equal(4, p.Count);
            Assert.Equal(new double[] { 0, 0, 1 }, p.AxisDir);
            Assert.Equal(0, Assert.Single(p.SourceFeatureIndices));
        }

        [Fact]
        public void Mirror_reads_plane_origin_and_normal_from_a_work_plane()
        {
            var yz = new FakeWorkPlane("YZ Plane", new double[] { 0, 0, 0 }, new double[] { 1, 0, 0 });
            var mirror = new List<MirrorFeature>
            {
                new FakeMirrorFeature("Mirror1", new[] { "Extrude1" }, yz),
            };

            InventorDocument ir = Extract(mirror: mirror);

            var m = Assert.IsType<InventorMirror>(ir.Features.Last());
            Assert.Equal(new double[] { 1, 0, 0 }, m.PlaneNormal);
            Assert.Equal(0, Assert.Single(m.SourceFeatureIndices));
        }

        [Fact]
        public void Pattern_referencing_an_unknown_source_is_skipped()
        {
            var xAxis = new FakeWorkAxis(new double[] { 0, 0, 0 }, new double[] { 1, 0, 0 });
            var rect = new List<RectangularPatternFeature>
            {
                new FakeRectangularPatternFeature("Pattern1", new[] { "Ghost" }, 3, 6, xAxis),
            };

            InventorDocument ir = Extract(rect: rect);

            Assert.DoesNotContain(ir.Features, f => f is InventorRectangularPattern);
        }
    }
}
