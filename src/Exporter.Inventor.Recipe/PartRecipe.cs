// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Oblikovati.Exporter.Inventor.Recipe
{
    /// <summary>
    /// The <c>model:</c> body of a part document. Mirrors partRecipe in
    /// Oblikovati/model/compdef/serialize.go. Sections grow milestone by milestone
    /// (sketches, features, etc.); empty sections are omitted.
    /// </summary>
    public sealed class PartRecipe
    {
        [YamlMember(Alias = "units")]
        public Units Units { get; set; } = new Units();

        /// <summary>Model parameters. Omitted when empty.</summary>
        [YamlMember(Alias = "parameters", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<ParameterRecipe> Parameters { get; } = new List<ParameterRecipe>();
    }

    /// <summary>
    /// Unit preferences. Field set mirrors the units map written by the Go codec.
    /// The kernel works internally in centimetres; these are the display/exchange
    /// units only.
    /// </summary>
    public sealed class Units
    {
        [YamlMember(Alias = "length")]
        public string Length { get; set; } = "mm";

        [YamlMember(Alias = "angle")]
        public string Angle { get; set; } = "deg";

        [YamlMember(Alias = "area")]
        public string Area { get; set; } = "mm^2";

        [YamlMember(Alias = "volume")]
        public string Volume { get; set; } = "mm^3";

        [YamlMember(Alias = "mass")]
        public string Mass { get; set; } = "kg";

        [YamlMember(Alias = "time")]
        public string Time { get; set; } = "s";
    }
}
