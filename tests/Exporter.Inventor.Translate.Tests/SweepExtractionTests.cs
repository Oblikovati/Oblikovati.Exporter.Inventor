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
    /// Exercises the REAL FeatureExtractor's sweep walk via a faked straight-segment path. The
    /// profile sketch "Profile" is extracted first; the sweep's profile names it, and its path's
    /// sketch-line segments give the 3D polyline.
    /// </summary>
    public sealed class SweepExtractionTests
    {
        [Fact]
        public void Sweep_resolves_its_profile_sketch_and_reads_the_straight_path_polyline()
        {
            var sketches = new List<PlanarSketch>
            {
                new FakePlanarSketch(
                    "Profile", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                    new FakePlane(new FakeUnitVector(0, 0, 1)),
                    new List<SketchLine>(), new List<SketchCircle> { new FakeSketchCircle(0, 0, 1) }),
            };
            // A two-segment path: (0,0,0) -> (0,0,5) -> (0,0,10).
            var path = new double[][]
            {
                new double[] { 0, 0, 0 }, new double[] { 0, 0, 5 }, new double[] { 0, 0, 10 },
            };
            var sweeps = new List<SweepFeature> { new FakeSweepFeature("Sweep1", "Profile", path) };
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                sketches, sweeps: sweeps);

            InventorDocument ir = new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument();

            var sweep = Assert.IsType<InventorSweep>(ir.Features.Last());
            Assert.Equal(0, sweep.ProfileSketchIndex); // "Profile"
            Assert.Equal(3, sweep.Path.Count); // chained: start + each segment end
            Assert.Equal(new double[] { 0, 0, 0 }, sweep.Path[0]);
            Assert.Equal(new double[] { 0, 0, 10 }, sweep.Path[2]);
        }

        [Fact]
        public void Sweep_tessellates_a_curved_path_segment_into_the_polyline()
        {
            var sketches = new List<PlanarSketch>
            {
                new FakePlanarSketch(
                    "Profile", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                    new FakePlane(new FakeUnitVector(0, 0, 1)),
                    new List<SketchLine>(), new List<SketchCircle> { new FakeSketchCircle(0, 0, 1) }),
            };
            // A line segment (0,0,0)->(0,0,5) then an arc tessellated as 3 stroke points up to (2,0,7).
            var arcStrokes = new double[][]
            {
                new double[] { 0, 0, 5 }, new double[] { 1, 0, 6 }, new double[] { 2, 0, 7 },
            };
            var path = new FakeEntityPath(new List<PathEntity>
            {
                new FakePathEntity(new double[] { 0, 0, 0 }, new double[] { 0, 0, 5 }),
                new FakePathEntity(new FakePathArc(arcStrokes), false),
            });
            var sweeps = new List<SweepFeature> { new FakeSweepFeature("Sweep1", "Profile", path) };
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                sketches, sweeps: sweeps);

            InventorDocument ir = new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument();

            var sweep = Assert.IsType<InventorSweep>(ir.Features.Last());
            // line start + line/arc shared junction (deduped) + 2 more arc strokes = 4 points.
            Assert.Equal(4, sweep.Path.Count);
            Assert.Equal(new double[] { 0, 0, 0 }, sweep.Path[0]);
            Assert.Equal(new double[] { 0, 0, 5 }, sweep.Path[1]);
            Assert.Equal(new double[] { 2, 0, 7 }, sweep.Path[3]);
        }

        [Fact]
        public void Sweep_reverses_a_curved_segment_when_opposed_to_the_sketch_entity()
        {
            var sketches = new List<PlanarSketch>
            {
                new FakePlanarSketch(
                    "Profile", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                    new FakePlane(new FakeUnitVector(0, 0, 1)),
                    new List<SketchLine>(), new List<SketchCircle> { new FakeSketchCircle(0, 0, 1) }),
            };
            var strokes = new double[][]
            {
                new double[] { 0, 0, 0 }, new double[] { 1, 0, 1 }, new double[] { 2, 0, 2 },
            };
            var path = new FakeEntityPath(new List<PathEntity>
            {
                new FakePathEntity(new FakePathSpline(strokes), true), // opposed -> reversed
            });
            var sweeps = new List<SweepFeature> { new FakeSweepFeature("Sweep1", "Profile", path) };
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                sketches, sweeps: sweeps);

            InventorDocument ir = new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument();

            var sweep = Assert.IsType<InventorSweep>(ir.Features.Last());
            Assert.Equal(3, sweep.Path.Count);
            Assert.Equal(new double[] { 2, 0, 2 }, sweep.Path[0]); // reversed start
            Assert.Equal(new double[] { 0, 0, 0 }, sweep.Path[2]);
        }
    }
}
