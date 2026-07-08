// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// A sheet-metal part is exported as its flat pattern extruded by the sheet thickness (its
    /// history has no readable solid features). Here a 4x4 flat plate, thickness 0.2, must produce
    /// a four-line sketch and a distance-0.2 extrude — the same volume as the folded part.
    /// </summary>
    public sealed class SheetMetalExtractionTests
    {
        [Fact]
        public void Extracts_flat_pattern_as_thickness_extrude()
        {
            double[][] corners =
            {
                new[] { 0.0, 0.0, 0.0 }, new[] { 4.0, 0.0, 0.0 },
                new[] { 4.0, 4.0, 0.0 }, new[] { 0.0, 4.0, 0.0 },
            };
            var sheet = new FakeSheetMetalDef(0.2, new FakeSheetFace(corners, new[] { 0.0, 0.0, 1.0 }));
            var ir = new InventorDocument();

            SheetMetalExtractor.Extract(sheet, ir);

            InventorSketch sketch = Assert.Single(ir.Sketches);
            Assert.Equal(4, sketch.Curves.Count); // one line per flat-pattern edge

            InventorExtrude extrude = Assert.IsType<InventorExtrude>(Assert.Single(ir.Features));
            Assert.Equal(InventorOperation.NewBody, extrude.Operation);
            Assert.Equal(InventorExtentKind.Distance, extrude.ExtentKind);
            Assert.Equal(0.2, extrude.Distance);
            Assert.Single(extrude.ProfileSeeds); // the plate seed (top-face centroid)
        }
    }

    public sealed class FakeSheetMetalDef : SheetMetalComponentDefinition
    {
        private readonly double _thickness;
        private readonly FlatPattern _flat;
        public FakeSheetMetalDef(double thickness, Face top)
        {
            _thickness = thickness;
            _flat = new FakeFlatPattern(top);
        }
        public override bool HasFlatPattern => true;
        public override FlatPattern FlatPattern => _flat;
        public override Parameter Thickness => new FakeDistanceParameter(_thickness);
    }

    public sealed class FakeFlatPattern : FlatPattern
    {
        private readonly Face _top;
        public FakeFlatPattern(Face top) => _top = top;
        public override Face TopFace => _top;
    }

    public sealed class FakeSheetFace : Face
    {
        private readonly Plane _plane;
        private readonly Vertices _vertices;
        private readonly Edges _edges;
        public FakeSheetFace(double[][] corners, double[] normal)
        {
            _plane = new FakeWorkPlaneGeometry(corners[0], normal);
            var verts = new List<Vertex>();
            var edges = new List<Edge>();
            for (int i = 0; i < corners.Length; i++)
            {
                verts.Add(new FakeBrepVertex(corners[i]));
                edges.Add(new FakeBrepEdge(corners[i], corners[(i + 1) % corners.Length]));
            }
            _vertices = new FakeVertices(verts);
            _edges = new FakeEdges(edges);
        }
        public override object Geometry => _plane;
        public override Vertices Vertices => _vertices;
        public override Edges Edges => _edges;
        public override bool IsParamReversed => false;
    }

    public sealed class FakeEdges : Edges
    {
        private readonly IList<Edge> _items;
        public FakeEdges(IList<Edge> items) => _items = items;
        public override int Count => _items.Count;
        public override Edge this[int index] => _items[index - 1];
    }
}
