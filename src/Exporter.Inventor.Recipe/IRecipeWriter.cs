// SPDX-License-Identifier: GPL-2.0-only
namespace Oblikovati.Exporter.Inventor.Recipe
{
    /// <summary>
    /// Renders an <see cref="OblikovatiDocument"/> to its on-disk YAML text. Owned by
    /// this project so callers never depend on the underlying YAML library directly.
    /// </summary>
    public interface IRecipeWriter
    {
        /// <summary>
        /// Returns the full YAML document text for <paramref name="document"/>.
        /// </summary>
        string Write(OblikovatiDocument document);
    }
}
