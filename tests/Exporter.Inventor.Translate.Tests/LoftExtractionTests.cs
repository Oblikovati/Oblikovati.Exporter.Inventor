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
    /// Exercises the REAL FeatureExtractor's loft walk via faked loft sections. The two sketches
    /// "Bottom"/"Top" are extracted first; the loft's section profiles name them, so the extractor
    /// resolves each section to its sketch index.
    /// </summary>
    public sealed class LoftExtractionTests
    {
        [Fact]
        public void Loft_resolves_its_section_profiles_to_sketch_indices()
        {
            var sketches = new List<PlanarSketch>
            {
                NamedCircleSketch("Bottom"),
                NamedCircleSketch("Top"),
            };
            var lofts = new List<LoftFeature> { new FakeLoftFeature("Loft1", "Bottom", "Top") };
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                sketches, lofts: lofts);

            InventorDocument ir = new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument();

            var loft = Assert.IsType<InventorLoft>(ir.Features.Last());
            Assert.Equal(2, loft.Sections.Count);
            Assert.Equal(0, loft.Sections[0].SketchIndex); // "Bottom"
            Assert.Equal(1, loft.Sections[1].SketchIndex); // "Top"
        }

        // A one-circle sketch with the given name (the extractor reads its name for section mapping).
        private static FakePlanarSketch NamedCircleSketch(string name) => new FakePlanarSketch(
            name, new FakePoint(0, 0, 0), new FakeLine(new FakeUnitVector(1, 0, 0)),
            new FakePlane(new FakeUnitVector(0, 0, 1)),
            new List<SketchLine>(), new List<SketchCircle> { new FakeSketchCircle(0, 0, 1) });
    }
}
