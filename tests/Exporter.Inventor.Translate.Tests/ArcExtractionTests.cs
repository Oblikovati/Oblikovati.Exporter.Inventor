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
    /// Exercises the REAL SketchExtractor's arc reading + arc-aware coincidence inference via a
    /// faked half-disc sketch (a diameter line closed by a semicircular arc).
    /// </summary>
    public sealed class ArcExtractionTests
    {
        private static InventorSketch ExtractHalfDisc()
        {
            var lines = new List<SketchLine> { FakeSketchLine.From(-2, 0, 2, 0) };
            // Upper semicircle: center (0,0), start (2,0), end (-2,0), positive (CCW) sweep.
            var arcs = new List<SketchArc> { new FakeSketchArc(0, 0, 2, 0, -2, 0, 3.14159) };
            var sketch = new FakePlanarSketch(
                "HalfDisc", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                new FakePlane(new FakeUnitVector(0, 0, 1)), lines, new List<SketchCircle>(), arcs: arcs);
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                new List<PlanarSketch> { sketch });
            return new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument().Sketches[0];
        }

        [Fact]
        public void Extracts_an_arc_with_center_start_end_and_ccw()
        {
            InventorSketch sk = ExtractHalfDisc();

            InventorCurve arc = Assert.Single(sk.Curves, c => c.Kind == InventorCurveKind.Arc);
            Assert.Equal(new double[] { 0, 0 }, arc.Center);
            Assert.Equal(new double[] { 2, 0 }, arc.Start);
            Assert.Equal(new double[] { -2, 0 }, arc.End);
            Assert.True(arc.Ccw); // positive sweep angle
        }

        [Fact]
        public void Infers_coincidences_joining_the_arc_ends_to_the_diameter_line()
        {
            InventorSketch sk = ExtractHalfDisc();

            // The line's two ends meet the arc's two ends -> two inferred coincidences.
            Assert.Equal(2, sk.Constraints.Count(c => c.Kind == InventorConstraintKind.Coincident));
        }
    }
}
