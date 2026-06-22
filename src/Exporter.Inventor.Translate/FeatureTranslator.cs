// SPDX-License-Identifier: GPL-2.0-only
using System;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>
    /// Translates Inventor history features into Oblikovati recipe features. Unsupported kinds
    /// are recorded in the report and skipped (never STEP-substituted). Currently handles
    /// extrudes; revolve/sweep/loft and patterns/mirror follow. Distances pass through (cm).
    /// </summary>
    public sealed class FeatureTranslator
    {
        private readonly ExportReport _report;

        public FeatureTranslator(ExportReport report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        /// <summary>Returns the recipe feature for <paramref name="feature"/>, or null if unsupported.</summary>
        public FeatureData? Translate(InventorFeature feature)
        {
            switch (feature)
            {
                case InventorExtrude extrude:
                    return TranslateExtrude(extrude);
                default:
                    _report.Skip("feature", feature.GetType().Name);
                    return null;
            }
        }

        private static FeatureData TranslateExtrude(InventorExtrude extrude)
        {
            var payload = new ExtrudeData
            {
                Sketch = extrude.SketchIndex,
                Operation = OperationName(extrude.Operation),
                Extent = "distance",
                Direction = DirectionName(extrude.Direction),
                Distance = extrude.Distance,
                Distance2 = extrude.SecondDistance != 0 ? extrude.SecondDistance : (double?)null,
                Taper = extrude.TaperRadians != 0 ? extrude.TaperRadians : (double?)null,
            };
            payload.Profiles.Add(extrude.ProfileIndex);

            return new FeatureData
            {
                Kind = "extrude",
                Name = extrude.Name.Length == 0 ? null : extrude.Name,
                Extrude = payload,
            };
        }

        private static string OperationName(InventorOperation operation) => operation switch
        {
            InventorOperation.Join => "join",
            InventorOperation.Cut => "cut",
            InventorOperation.Intersect => "intersect",
            _ => "newBody",
        };

        private static string DirectionName(InventorExtentDirection direction) => direction switch
        {
            InventorExtentDirection.Negative => "negative",
            InventorExtentDirection.Symmetric => "symmetric",
            _ => "positive",
        };
    }
}
