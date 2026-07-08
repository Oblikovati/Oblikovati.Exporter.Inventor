// SPDX-License-Identifier: GPL-2.0-only

using Oblikovati.Exporter.Inventor.Emit;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Emit.Tests
{
    public class WorkPlaneMapperTests
    {
        // A non-origin datum (offset origin, still-orthonormal axes) maps to a fixed-frame work
        // plane carrying that exact frame — origin + both in-plane axes verbatim.
        [Fact]
        public void Maps_a_non_origin_datum_to_a_fixed_frame()
        {
            var sketch = new InventorSketch
            {
                Origin = new double[] { 1, 2, 3 },
                XAxis = new double[] { 0, 1, 0 },
                YAxis = new double[] { 0, 0, 1 },
            };

            Assert.True(WorkPlaneMapper.TryMap(sketch, out WorkPlaneSpec spec));
            Assert.Equal("fixed-frame", spec.Kind);
            Assert.Equal(new double[] { 1, 2, 3 }, spec.Origin);
            Assert.Equal(new double[] { 0, 1, 0 }, spec.XAxis);
            Assert.Equal(new double[] { 0, 0, 1 }, spec.YAxis);
        }

        // A tilted-but-orthonormal datum still maps faithfully (the frozen frame carries any
        // orientation).
        [Fact]
        public void Maps_a_tilted_orthonormal_datum()
        {
            const double s = 0.70710678118;
            var sketch = new InventorSketch
            {
                Origin = new double[] { 0, 0, 5 },
                XAxis = new double[] { s, s, 0 },
                YAxis = new double[] { -s, s, 0 },
            };

            Assert.True(WorkPlaneMapper.TryMap(sketch, out WorkPlaneSpec spec));
            Assert.Equal(sketch.XAxis, spec.XAxis);
        }

        // A degenerate frame (non-unit axis) would distort the fixed-frame constructor, so it maps
        // to nothing — the caller defers the sketch (correctness over coverage).
        [Fact]
        public void Defers_a_non_unit_axis_frame()
        {
            var sketch = new InventorSketch
            {
                XAxis = new double[] { 2, 0, 0 },
                YAxis = new double[] { 0, 1, 0 },
            };

            Assert.False(WorkPlaneMapper.TryMap(sketch, out _));
        }

        // Non-orthogonal axes cannot form a valid plane frame — defer.
        [Fact]
        public void Defers_a_non_orthogonal_frame()
        {
            var sketch = new InventorSketch
            {
                XAxis = new double[] { 1, 0, 0 },
                YAxis = new double[] { 0.70710678118, 0.70710678118, 0 },
            };

            Assert.False(WorkPlaneMapper.TryMap(sketch, out _));
        }
    }
}
