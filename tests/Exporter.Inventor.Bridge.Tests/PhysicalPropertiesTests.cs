// SPDX-License-Identifier: GPL-2.0-only

using System.Text.Json;
using Oblikovati.Exporter.Inventor.Bridge;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Bridge.Tests
{
    public sealed class PhysicalPropertiesTests
    {
        private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

        [Fact]
        public void Reads_volume_from_the_live_get_physical_properties_shape()
        {
            // Captured verbatim from the bridge's get_physical_properties result for the
            // 40x30mm rectangle extruded 50mm (units: cm / cm^3).
            JsonElement props = Json("{\"mass\":0,\"volume\":60,\"area\":94,\"density\":0,\"centroid\":[2,1.5,2.5]}");

            Assert.True(PhysicalProperties.TryReadVolumeCm3(props, out double volume));
            Assert.Equal(60.0, volume, precision: 6);
        }

        [Fact]
        public void Reads_a_fractional_volume()
        {
            JsonElement props = Json("{\"volume\":31.4159}");

            Assert.True(PhysicalProperties.TryReadVolumeCm3(props, out double volume));
            Assert.Equal(31.4159, volume, precision: 4);
        }

        [Fact]
        public void Reads_volume_nested_one_level_deep()
        {
            JsonElement props = Json("{\"physicalProperties\":{\"volume\":12.5}}");

            Assert.True(PhysicalProperties.TryReadVolumeCm3(props, out double volume));
            Assert.Equal(12.5, volume, precision: 6);
        }

        [Fact]
        public void Reports_missing_volume()
        {
            JsonElement props = Json("{\"mass\":0,\"area\":94}");

            Assert.False(PhysicalProperties.TryReadVolumeCm3(props, out double volume));
            Assert.Equal(0.0, volume);
        }

        [Fact]
        public void Ignores_a_non_numeric_volume()
        {
            JsonElement props = Json("{\"volume\":\"lots\"}");

            Assert.False(PhysicalProperties.TryReadVolumeCm3(props, out _));
        }
    }
}
