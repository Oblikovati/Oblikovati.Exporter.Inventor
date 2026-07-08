// SPDX-License-Identifier: GPL-2.0-only

using System.Text.Json;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// The host's reply to an <c>add_feature</c> call: the created feature's name (used to reference
    /// it from a later pattern/mirror), whether the recompute was healthy, and, when it was not, the
    /// host's reason. An unhealthy feature still exists in the model (its volume is whatever the host
    /// built), so the emitter surfaces the reason rather than silently trusting the geometry.
    /// </summary>
    public sealed class FeatureAddResult
    {
        public string Name { get; }

        public bool Healthy { get; }

        public string Reason { get; }

        public int Bodies { get; }

        public FeatureAddResult(string name, bool healthy, string reason, int bodies)
        {
            Name = name;
            Healthy = healthy;
            Reason = reason;
            Bodies = bodies;
        }

        /// <summary>Parses the {feature, healthy, reason, bodies} object add_feature returns.</summary>
        public static FeatureAddResult Parse(JsonElement result)
        {
            if (result.ValueKind != JsonValueKind.Object)
                return new FeatureAddResult(string.Empty, true, string.Empty, 0);
            string name = String(result, "feature");
            bool healthy = !result.TryGetProperty("healthy", out JsonElement h) || h.ValueKind != JsonValueKind.False;
            string reason = String(result, "reason");
            int bodies = result.TryGetProperty("bodies", out JsonElement b) && b.ValueKind == JsonValueKind.Number ? b.GetInt32() : 0;
            return new FeatureAddResult(name, healthy, reason, bodies);
        }

        private static string String(JsonElement obj, string field) =>
            obj.TryGetProperty(field, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : string.Empty;
    }
}
