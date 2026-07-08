// SPDX-License-Identifier: GPL-2.0-only
using YamlDotNet.Serialization;

namespace Oblikovati.Exporter.Inventor.Recipe
{
    /// <summary>
    /// One work feature. Mirrors WorkFeatureData in Oblikovati/model/feature/serialize_work.go.
    /// A fixed-frame plane is self-contained (origin + two in-plane axes), so it needs no
    /// references. Position is in centimetre database units; axes are unit vectors.
    /// </summary>
    public sealed class WorkFeatureData
    {
        [YamlMember(Alias = "collection")]
        public string Collection { get; set; } = "plane";

        [YamlMember(Alias = "kind")]
        public string Kind { get; set; } = "fixed-frame";

        [YamlMember(Alias = "position", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double[]? Position { get; set; }

        [YamlMember(Alias = "xaxis", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double[]? XAxis { get; set; }

        [YamlMember(Alias = "yaxis", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double[]? YAxis { get; set; }
    }
}
