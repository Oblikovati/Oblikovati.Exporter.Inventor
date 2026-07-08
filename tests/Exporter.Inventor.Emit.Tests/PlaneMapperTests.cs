// SPDX-License-Identifier: GPL-2.0-only

using Oblikovati.Exporter.Inventor.Emit;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Emit.Tests
{
    public class PlaneMapperTests
    {
        [Fact]
        public void Maps_the_default_axes_to_XY()
        {
            var sketch = new InventorSketch();
            Assert.True(PlaneMapper.TryMapOriginPlane(sketch, out string plane));
            Assert.Equal("XY", plane);
        }

        [Fact]
        public void Maps_XZ_and_YZ_origin_frames()
        {
            var xz = new InventorSketch { XAxis = new double[] { 1, 0, 0 }, YAxis = new double[] { 0, 0, 1 } };
            var yz = new InventorSketch { XAxis = new double[] { 0, 1, 0 }, YAxis = new double[] { 0, 0, 1 } };

            Assert.True(PlaneMapper.TryMapOriginPlane(xz, out string xzName));
            Assert.True(PlaneMapper.TryMapOriginPlane(yz, out string yzName));
            Assert.Equal("XZ", xzName);
            Assert.Equal("YZ", yzName);
        }

        [Fact]
        public void Defers_a_sketch_off_the_origin()
        {
            var offset = new InventorSketch { Origin = new double[] { 0, 0, 5 } };
            Assert.False(PlaneMapper.TryMapOriginPlane(offset, out _));
        }

        [Fact]
        public void Defers_a_tilted_sketch()
        {
            var tilted = new InventorSketch { XAxis = new double[] { 0.707, 0.707, 0 }, YAxis = new double[] { -0.707, 0.707, 0 } };
            Assert.False(PlaneMapper.TryMapOriginPlane(tilted, out _));
        }
    }
}
