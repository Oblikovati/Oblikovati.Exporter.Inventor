// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Oblikovati.Exporter.Inventor.Recipe
{
    /// <summary>
    /// The <c>model:</c> body of an assembly document. Mirrors assemblyRecipe in
    /// Oblikovati/model/compdef/assembly_serialize.go: display units plus the placed
    /// occurrences. Component documents are referenced by file name, not embedded.
    /// </summary>
    public sealed class AssemblyRecipe
    {
        [YamlMember(Alias = "units")]
        public Units Units { get; set; } = new Units();

        [YamlMember(Alias = "occurrences", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<OccurrenceData> Occurrences { get; } = new List<OccurrenceData>();
    }

    /// <summary>
    /// One placement. Mirrors occurrenceRecipe: the instance name, the component document it
    /// references (an owner-relative file name), and a 16-cell row-major transform whose
    /// translation (cells 3, 7, 11) is in centimetre database units.
    /// </summary>
    public sealed class OccurrenceData
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "component")]
        public string Component { get; set; } = string.Empty;

        [YamlMember(Alias = "transform", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public double[]? Transform { get; set; }
    }
}
