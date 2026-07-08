// SPDX-License-Identifier: GPL-2.0-only
using YamlDotNet.Serialization;

namespace Oblikovati.Exporter.Inventor.Recipe
{
    /// <summary>
    /// The serialized root of an Oblikovati document (.opd / .oad). Field names and
    /// shape mirror the Go codec in Oblikovati/model/compdef/serialize.go and
    /// persistence/yamlcodec; <see cref="IRecipeWriter"/> renders this to YAML.
    /// </summary>
    public sealed class OblikovatiDocument
    {
        /// <summary>Schema version written by the current Oblikovati build (manifest.go).</summary>
        [YamlMember(Alias = "schemaVersion")]
        public int SchemaVersion { get; set; } = 2;

        /// <summary>1 = part (.opd), 2 = assembly (.oad). See API types/document_type.go.</summary>
        [YamlMember(Alias = "documentType")]
        public int DocumentType { get; set; } = 1;

        [YamlMember(Alias = "displayName")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// The recipe body. A <see cref="PartRecipe"/> for parts; the assembly recipe
        /// arrives in a later milestone. Serialized as the nested <c>model:</c> mapping.
        /// </summary>
        [YamlMember(Alias = "model")]
        public object? Model { get; set; }
    }
}
