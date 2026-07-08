// SPDX-License-Identifier: GPL-2.0-only
using YamlDotNet.Serialization;

namespace Oblikovati.Exporter.Inventor.Recipe
{
    /// <summary>
    /// <see cref="IRecipeWriter"/> backed by YamlDotNet. Honours the explicit
    /// <c>[YamlMember(Alias=...)]</c> keys on the recipe POCOs and omits null members
    /// so the output matches the Go codec's omitempty behaviour. The kept YamlDotNet
    /// instance is immutable and reusable.
    /// </summary>
    public sealed class RecipeYamlWriter : IRecipeWriter
    {
        private readonly ISerializer _serializer;

        public RecipeYamlWriter()
        {
            _serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .DisableAliases()
                .Build();
        }

        public string Write(OblikovatiDocument document)
        {
            return _serializer.Serialize(document);
        }
    }
}
