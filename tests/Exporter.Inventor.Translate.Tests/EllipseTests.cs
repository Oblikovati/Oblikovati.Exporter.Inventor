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
    public sealed class EllipseTests
    {
        [Fact]
        public void Translates_an_ellipse_to_centre_axis_and_radii()
        {
            OblikovatiDocument recipe =
                new DocumentTranslator().Translate(InventorSampleParts.EllipsePart(), new ExportReport());
            var part = (PartRecipe)recipe.Model!;
            EntityData ellipse = Assert.Single(part.Sketches[0].Entities, e => e.Kind == "ellipse");

            Assert.Single(ellipse.Points); // centre only
            Assert.Equal(new double[] { 1, 0 }, ellipse.MajorAxis);
            Assert.Equal(4.0, ellipse.MajorRadius);
            Assert.Equal(2.0, ellipse.MinorRadius);
        }

        [Fact]
        public void Translates_an_elliptical_arc_with_start_and_end_angles()
        {
            OblikovatiDocument recipe =
                new DocumentTranslator().Translate(InventorSampleParts.EllipticalArcPart(), new ExportReport());
            var part = (PartRecipe)recipe.Model!;
            EntityData arc = Assert.Single(part.Sketches[0].Entities, e => e.Kind == "ellipticalArc");

            Assert.Equal(4.0, arc.MajorRadius);
            Assert.Equal(2.0, arc.MinorRadius);
            Assert.Equal(0.0, arc.StartAngle);
            Assert.Equal(System.Math.PI, arc.EndAngle!.Value, 6);
        }

        [Fact]
        public void Extracts_an_elliptical_arc_with_end_angle_from_start_plus_sweep()
        {
            var arcs = new List<SketchEllipticalArc>
            {
                // start 0, sweep π -> end π.
                new FakeSketchEllipticalArc(0, 0, new double[] { 1, 0 }, 4, 2, 0, System.Math.PI),
            };
            var sketch = new FakePlanarSketch(
                "EA", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                new FakePlane(new FakeUnitVector(0, 0, 1)),
                new List<SketchLine>(), new List<SketchCircle>(), ellipticalArcs: arcs);
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                new List<PlanarSketch> { sketch });

            InventorSketch sk =
                new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument().Sketches[0];

            InventorCurve arc = Assert.Single(sk.Curves, c => c.Kind == InventorCurveKind.EllipticalArc);
            Assert.Equal(0.0, arc.StartAngle);
            Assert.Equal(System.Math.PI, arc.EndAngle, 6);
        }

        [Fact]
        public void Extracts_an_ellipse_from_the_real_extractor()
        {
            var ellipses = new List<SketchEllipse>
            {
                new FakeSketchEllipse(0, 0, new double[] { 1, 0 }, 4, 2),
            };
            var sketch = new FakePlanarSketch(
                "E", new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
                new FakePlane(new FakeUnitVector(0, 0, 1)),
                new List<SketchLine>(), new List<SketchCircle>(), ellipses: ellipses);
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                new List<PlanarSketch> { sketch });

            InventorSketch sk =
                new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument().Sketches[0];

            InventorCurve ellipse = Assert.Single(sk.Curves, c => c.Kind == InventorCurveKind.Ellipse);
            Assert.Equal(new double[] { 1, 0 }, ellipse.MajorAxis);
            Assert.Equal(4.0, ellipse.MajorRadius);
            Assert.Equal(2.0, ellipse.MinorRadius);
        }
    }
}
