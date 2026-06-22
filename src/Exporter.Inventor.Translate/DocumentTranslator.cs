// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>
    /// Maps the Inventor-neutral IR (<see cref="InventorDocument"/>) to a serializable
    /// Oblikovati recipe (<see cref="OblikovatiDocument"/>). Pure and host-free; the
    /// translation registry (sketches, features, datums…) grows on this seam in later
    /// milestones. For now it carries the envelope, units, and model parameters.
    /// </summary>
    public sealed class DocumentTranslator
    {
        /// <summary>
        /// Translates <paramref name="source"/> into a recipe, appending anything it cannot
        /// carry to <paramref name="report"/>.
        ///
        /// Example:
        /// <code>
        /// var recipe = new DocumentTranslator().Translate(doc, new ExportReport());
        /// </code>
        /// </summary>
        public OblikovatiDocument Translate(InventorDocument source, ExportReport report)
        {
            var part = new PartRecipe();
            part.Units.Length = source.LengthUnit;
            part.Units.Angle = source.AngleUnit;
            TranslateParameters(source, part);

            return new OblikovatiDocument
            {
                DocumentType = (int)source.Kind,
                DisplayName = source.DisplayName,
                Model = part,
            };
        }

        private static void TranslateParameters(InventorDocument source, PartRecipe part)
        {
            foreach (InventorParameter p in source.Parameters)
            {
                part.Parameters.Add(new ParameterRecipe
                {
                    Name = p.Name,
                    Kind = "model",
                    Expression = p.Expression,
                });
            }
        }
    }
}
