// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using System.Linq;
using Inventor;
using Oblikovati.Exporter.Inventor.Fixtures;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;
using Oblikovati.Exporter.Inventor.Translate;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Exercises the REAL SketchExtractor's spline reading via a faked closed fit-spline, and the
    /// translator's spline emission.
    /// </summary>
    public sealed class SplineExtractionTests
    {
        private static InventorSketch ExtractBlob()
        {
            var pts = new double[][]
            {
                new double[] { 0, 0 }, new double[] { 4, 0 }, new double[] { 4, 3 }, new double[] { 0, 3 },
            };
            var splines = new List<SketchSpline> { new FakeSketchSpline(true, pts) };
            var sketch = new FakePlanarSketch(
                "Blob", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                new FakePlane(new FakeUnitVector(0, 0, 1)),
                new List<SketchLine>(), new List<SketchCircle>(), splines: splines);
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                new List<PlanarSketch> { sketch });
            return new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument().Sketches[0];
        }

        [Fact]
        public void Extracts_a_closed_fit_spline_with_its_points()
        {
            InventorSketch sk = ExtractBlob();

            InventorCurve spline = Assert.Single(sk.Curves, c => c.Kind == InventorCurveKind.Spline);
            Assert.Equal(4, spline.SplinePoints.Count);
            Assert.True(spline.Closed);
            Assert.True(spline.Fit);
            Assert.Equal(new double[] { 4, 3 }, spline.SplinePoints[2]);
        }

        [Fact]
        public void Extracts_a_closed_control_point_spline_as_a_non_fit_spline()
        {
            var pts = new double[][]
            {
                new double[] { 0, 0 }, new double[] { 4, 0 }, new double[] { 4, 3 }, new double[] { 0, 3 },
            };
            var ctrl = new List<SketchControlPointSpline> { new FakeSketchControlPointSpline(true, pts) };
            var sketch = new FakePlanarSketch(
                "Ctrl", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                new FakePlane(new FakeUnitVector(0, 0, 1)),
                new List<SketchLine>(), new List<SketchCircle>(), controlPointSplines: ctrl);
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                new List<PlanarSketch> { sketch });

            InventorSketch sk =
                new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument().Sketches[0];

            InventorCurve spline = Assert.Single(sk.Curves, c => c.Kind == InventorCurveKind.Spline);
            Assert.Equal(4, spline.SplinePoints.Count);
            Assert.True(spline.Closed);
            Assert.False(spline.Fit); // control-point, not interpolating
        }

        [Fact]
        public void Translates_a_spline_to_a_spline_entity_with_all_its_points()
        {
            OblikovatiDocument recipe =
                new DocumentTranslator().Translate(InventorSampleParts.SplineSketchPart(), new ExportReport());
            var part = (PartRecipe)recipe.Model!;
            EntityData spline = Assert.Single(part.Sketches[0].Entities, e => e.Kind == "spline");

            Assert.Equal(4, spline.Points.Count);
            Assert.True(spline.Closed);
            Assert.True(spline.Fit);
            // Each fit point is fixed -> four fix constraints (DOF 0).
            Assert.Equal(4, part.Sketches[0].Constraints.Count(c => c.Kind == "fix"));
        }
    }
}
