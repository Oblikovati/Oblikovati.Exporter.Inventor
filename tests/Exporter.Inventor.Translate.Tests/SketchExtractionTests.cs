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
    /// Exercises the REAL <see cref="SketchExtractor"/> against a faked Inventor sketch (the
    /// fakes subclass the stub), so the extraction logic runs end to end with no Inventor install.
    /// </summary>
    public sealed class SketchExtractionTests
    {
        private static InventorDocument ExtractSquare()
        {
            var sketches = new List<PlanarSketch> { FakePlanarSketch.Square() };
            var doc = new FakePartDocument(
                "plate.ipt", @"C:\work\plate.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(), sketches);
            var adapter = new InventorSessionAdapter(new FakeInventorApplication(doc));
            return adapter.ExtractActiveDocument();
        }

        [Fact]
        public void Extracts_four_lines_with_the_real_plane_frame()
        {
            InventorDocument ir = ExtractSquare();

            InventorSketch sketch = Assert.Single(ir.Sketches);
            Assert.Equal("Square", sketch.Name);
            Assert.Equal(4, sketch.Curves.Count(c => c.Kind == InventorCurveKind.Line));
            // Frame: X from the axis line, Y = normal (0,0,1) x X (1,0,0) = (0,1,0).
            Assert.Equal(new double[] { 1, 0, 0 }, sketch.XAxis);
            Assert.Equal(new double[] { 0, 1, 0 }, sketch.YAxis);
        }

        [Fact]
        public void Infers_a_coincidence_at_each_shared_corner()
        {
            InventorDocument ir = ExtractSquare();

            InventorSketch sketch = ir.Sketches[0];
            // A closed quad shares four corners -> four inferred coincidences.
            Assert.Equal(4, sketch.Constraints.Count(c => c.Kind == InventorConstraintKind.Coincident));
        }
    }
}
