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
            TranslateWorkPlanes(source, part);
            TranslateSketches(source, part, report);
            TranslateFeatures(source, part, report);

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

        private static void TranslateWorkPlanes(InventorDocument source, PartRecipe part)
        {
            foreach (InventorWorkPlane plane in source.WorkPlanes)
            {
                part.WorkFeatures.Add(WorkPlaneTranslator.Translate(plane));
            }
        }

        // Sketches, their points and entities share one id space (matches the Go codec, where a
        // sketch's id precedes its points' and entities' ids), so one allocator threads through.
        private static void TranslateSketches(InventorDocument source, PartRecipe part, ExportReport report)
        {
            var ids = new IdAllocator();
            var translator = new SketchTranslator(ids, report);
            foreach (InventorSketch sketch in source.Sketches)
            {
                int sketchId = ids.Next();
                part.Sketches.Add(translator.Translate(sketch, sketchId));
            }
        }

        private static void TranslateFeatures(InventorDocument source, PartRecipe part, ExportReport report)
        {
            var translator = new FeatureTranslator(report);
            foreach (InventorFeature feature in source.Features)
            {
                FeatureData? translated = translator.Translate(feature);
                if (translated != null)
                {
                    part.Features.Add(translated);
                }
            }
        }
    }
}
