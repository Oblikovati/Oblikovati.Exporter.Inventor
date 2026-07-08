// SPDX-License-Identifier: GPL-2.0-only

using System;
using Oblikovati.Exporter.Inventor.Emit;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Emit.Tests
{
    public class FeatureMappingTests
    {
        [Theory]
        [InlineData(InventorOperation.NewBody, "new")]
        [InlineData(InventorOperation.Join, "join")]
        [InlineData(InventorOperation.Cut, "cut")]
        [InlineData(InventorOperation.Intersect, "intersect")]
        public void Maps_operation(InventorOperation op, string expected) =>
            Assert.Equal(expected, FeatureMapping.Operation(op));

        [Theory]
        [InlineData(InventorExtentKind.Distance, "distance")]
        [InlineData(InventorExtentKind.ThroughAll, "through-all")]
        [InlineData(InventorExtentKind.ToNext, "to-next")]
        [InlineData(InventorExtentKind.ToFace, "to-face")]
        public void Maps_extent(InventorExtentKind kind, string expected) =>
            Assert.Equal(expected, FeatureMapping.Extent(kind));

        [Theory]
        [InlineData(InventorExtentDirection.Positive, "positive")]
        [InlineData(InventorExtentDirection.Negative, "negative")]
        [InlineData(InventorExtentDirection.Symmetric, "symmetric")]
        public void Maps_direction(InventorExtentDirection dir, string expected) =>
            Assert.Equal(expected, FeatureMapping.Direction(dir));

        [Fact]
        public void Formats_centimetres_as_millimetres()
        {
            Assert.Equal("50 mm", FeatureMapping.Millimeters(5));
            Assert.Equal("16 mm", FeatureMapping.Millimeters(1.6));
        }

        [Fact]
        public void Formats_a_taper_angle_in_degrees()
        {
            Assert.Equal("3 deg", FeatureMapping.Degrees(3.0 * Math.PI / 180.0));
        }

        [Fact]
        public void Maps_a_zero_or_full_revolve_angle_to_360_degrees()
        {
            Assert.Equal("360 deg", FeatureMapping.RevolveAngle(0.0));
            Assert.Equal("360 deg", FeatureMapping.RevolveAngle(2.0 * Math.PI));
        }

        [Fact]
        public void Maps_a_partial_revolve_angle_to_its_degrees()
        {
            Assert.Equal("90 deg", FeatureMapping.RevolveAngle(Math.PI / 2.0));
            Assert.Equal("45 deg", FeatureMapping.RevolveAngle(Math.PI / 4.0));
        }
    }
}
