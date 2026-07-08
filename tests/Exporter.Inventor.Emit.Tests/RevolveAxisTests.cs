// SPDX-License-Identifier: GPL-2.0-only

using Oblikovati.Exporter.Inventor.Emit;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Emit.Tests
{
    public class RevolveAxisTests
    {
        // A vertical centerline (local x=0) on an origin XY sketch maps to the global Y axis
        // through the origin — the common turned-part case.
        [Fact]
        public void Maps_a_vertical_origin_XY_centerline_to_the_Y_axis()
        {
            var sketch = OriginXY(Centerline(0, -3, 0, 5));
            var revolve = new InventorRevolve { SketchIndex = 0 };

            Assert.True(RevolveAxis.TryResolve(sketch, revolve, out string axisRef));
            Assert.Equal("origin/axis/y", axisRef);
        }

        // A horizontal centerline (local y=0) on an origin XY sketch maps to the global X axis.
        [Fact]
        public void Maps_a_horizontal_origin_XY_centerline_to_the_X_axis()
        {
            var sketch = OriginXY(Centerline(-2, 0, 6, 0));
            var revolve = new InventorRevolve { SketchIndex = 0 };

            Assert.True(RevolveAxis.TryResolve(sketch, revolve, out string axisRef));
            Assert.Equal("origin/axis/x", axisRef);
        }

        // On an XZ sketch, a local-vertical centerline maps through the +Z in-plane axis to the
        // global Z axis — proving the mapping uses the true sketch frame, not the local axes.
        [Fact]
        public void Maps_through_the_sketch_frame_on_an_XZ_plane()
        {
            var sketch = new InventorSketch
            {
                XAxis = new double[] { 1, 0, 0 },
                YAxis = new double[] { 0, 0, 1 },
            };
            sketch.Curves.Add(Centerline(0, -1, 0, 4));
            var revolve = new InventorRevolve { SketchIndex = 0 };

            Assert.True(RevolveAxis.TryResolve(sketch, revolve, out string axisRef));
            Assert.Equal("origin/axis/z", axisRef);
        }

        // A centerline parallel to Y but offset (x=2) does NOT pass through the origin, so it maps
        // to no origin axis — the emitter must defer rather than revolve about the wrong axis.
        [Fact]
        public void Defers_an_offset_parallel_centerline()
        {
            var sketch = OriginXY(Centerline(2, -3, 2, 5));
            var revolve = new InventorRevolve { SketchIndex = 0 };

            Assert.False(RevolveAxis.TryResolve(sketch, revolve, out _));
        }

        // A tilted centerline is along no global axis — defer.
        [Fact]
        public void Defers_a_tilted_centerline()
        {
            var sketch = OriginXY(Centerline(0, 0, 3, 4));
            var revolve = new InventorRevolve { SketchIndex = 0 };

            Assert.False(RevolveAxis.TryResolve(sketch, revolve, out _));
        }

        // With several revolves sharing a sketch, AxisLineIndex selects THIS revolve's centerline
        // among the line-kind curves (matching the reader's Lines() order).
        [Fact]
        public void Selects_the_axis_line_by_index_when_a_sketch_has_several()
        {
            var sketch = OriginXY();
            sketch.Curves.Add(Centerline(3, -1, 3, 5)); // line 0: offset — not the origin axis
            sketch.Curves.Add(Centerline(0, -1, 0, 5)); // line 1: the Y axis
            var revolve = new InventorRevolve { SketchIndex = 0, AxisLineIndex = 1 };

            Assert.True(RevolveAxis.TryResolve(sketch, revolve, out string axisRef));
            Assert.Equal("origin/axis/y", axisRef);
        }

        // A shared sketch with more than one centerline and no AxisLineIndex is ambiguous — defer.
        [Fact]
        public void Defers_an_ambiguous_multi_centerline_sketch_without_an_index()
        {
            var sketch = OriginXY();
            sketch.Curves.Add(Centerline(0, -1, 0, 5));
            sketch.Curves.Add(Centerline(2, -1, 2, 5));
            var revolve = new InventorRevolve { SketchIndex = 0 }; // AxisLineIndex defaults to -1

            Assert.False(RevolveAxis.TryResolve(sketch, revolve, out _));
        }

        private static InventorSketch OriginXY(params InventorCurve[] curves)
        {
            var sketch = new InventorSketch();
            foreach (InventorCurve c in curves)
                sketch.Curves.Add(c);
            return sketch;
        }

        private static InventorCurve Centerline(double x0, double y0, double x1, double y1) => new InventorCurve
        {
            Kind = InventorCurveKind.Line,
            Centerline = true,
            Start = new[] { x0, y0 },
            End = new[] { x1, y1 },
        };
    }
}
