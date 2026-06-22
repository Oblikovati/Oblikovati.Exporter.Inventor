// SPDX-License-Identifier: GPL-2.0-only
using YamlDotNet.Serialization;

namespace Oblikovati.Exporter.Inventor.Recipe
{
    /// <summary>
    /// One parameter row. Mirrors parameterRecipe in
    /// Oblikovati/model/compdef/serialize.go. Editable parameters (kind user/model)
    /// carry an <see cref="Expression"/>; the expression string holds units inline
    /// (e.g. "4 cm") and may reference other parameters by name (e.g. "width * 2").
    /// </summary>
    public sealed class ParameterRecipe
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>One of: user | model | reference | derived | table.</summary>
        [YamlMember(Alias = "kind")]
        public string Kind { get; set; } = "user";

        [YamlMember(Alias = "expression", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Expression { get; set; }

        [YamlMember(Alias = "comment", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Comment { get; set; }
    }
}
