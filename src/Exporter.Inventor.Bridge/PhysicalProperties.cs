// SPDX-License-Identifier: GPL-2.0-only

using System.Text.Json;

namespace Oblikovati.Exporter.Inventor.Bridge
{
    /// <summary>
    /// Reads values out of a <c>get_physical_properties</c> tool result. The bridge reports
    /// lengths/volumes in centimetre units (cm, cm^3), so a callers' oracle comparison must be
    /// in the same units.
    /// </summary>
    public static class PhysicalProperties
    {
        /// <summary>
        /// Extracts the model volume in cm^3 from a physical-properties result. Accepts the value
        /// as a top-level <c>volume</c> number or nested one level under a properties object.
        /// </summary>
        public static bool TryReadVolumeCm3(JsonElement properties, out double volumeCm3)
        {
            return TryReadNumber(properties, "volume", out volumeCm3);
        }

        private static bool TryReadNumber(JsonElement element, string propertyName, out double value)
        {
            value = 0;
            if (element.ValueKind != JsonValueKind.Object)
                return false;

            if (element.TryGetProperty(propertyName, out JsonElement direct) &&
                direct.ValueKind == JsonValueKind.Number &&
                direct.TryGetDouble(out value))
            {
                return true;
            }

            foreach (JsonProperty child in element.EnumerateObject())
            {
                if (child.Value.ValueKind == JsonValueKind.Object &&
                    TryReadNumber(child.Value, propertyName, out value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
