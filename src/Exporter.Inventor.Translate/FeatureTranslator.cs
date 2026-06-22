// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>
    /// Translates Inventor history features into Oblikovati recipe features. Unsupported kinds
    /// are recorded in the report and skipped (never STEP-substituted). Distances pass through
    /// (cm); angles pass through (radians).
    /// </summary>
    public sealed class FeatureTranslator
    {
        private const double FullCircle = 2 * Math.PI;

        private readonly ExportReport _report;

        public FeatureTranslator(ExportReport report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        /// <summary>
        /// Returns the recipe feature for <paramref name="feature"/>, or null if unsupported or
        /// (for a pattern/mirror) one of its sources was itself skipped. <paramref name="sourceIndex"/>
        /// maps an IR feature index to the recipe feature index.
        /// </summary>
        public FeatureData? Translate(InventorFeature feature, IReadOnlyDictionary<int, int> sourceIndex)
        {
            switch (feature)
            {
                case InventorExtrude extrude:
                    return TranslateExtrude(extrude);
                case InventorRevolve revolve:
                    return TranslateRevolve(revolve);
                case InventorRectangularPattern rect:
                    return TranslateRectPattern(rect, sourceIndex);
                case InventorCircularPattern circ:
                    return TranslateCircPattern(circ, sourceIndex);
                case InventorMirror mirror:
                    return TranslateMirror(mirror, sourceIndex);
                case InventorFillet fillet:
                    return DressUpTranslator.Fillet(fillet);
                case InventorChamfer chamfer:
                    return DressUpTranslator.Chamfer(chamfer);
                case InventorShell shell:
                    return DressUpTranslator.Shell(shell);
                case InventorDraft draft:
                    return DressUpTranslator.Draft(draft);
                case InventorHole hole:
                    return DressUpTranslator.Hole(hole);
                case InventorSweep sweep:
                    return TranslateSweep(sweep);
                case InventorLoft loft:
                    return TranslateLoft(loft);
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
            return new FeatureData { Kind = "extrude", Name = NameOf(extrude), Extrude = payload };
        }

        private static FeatureData TranslateRevolve(InventorRevolve revolve)
        {
            // Own-centerline mode: the profile sketch carries the axis as a centerline, so no
            // axis fields are emitted. Angle 0 (full revolution) is left unset.
            var payload = new RevolveData
            {
                Sketch = revolve.SketchIndex,
                Profile = revolve.ProfileIndex,
                Operation = OperationName(revolve.Operation),
                Angle = revolve.AngleRadians != 0 ? revolve.AngleRadians : (double?)null,
            };
            return new FeatureData { Kind = "revolve", Name = NameOf(revolve), Revolve = payload };
        }

        private static FeatureData TranslateSweep(InventorSweep sweep)
        {
            var payload = new SweepData
            {
                Sketch = sweep.ProfileSketchIndex,
                Profile = sweep.ProfileIndex,
                Closed = sweep.Closed ? true : (bool?)null,
                Operation = OperationName(sweep.Operation),
            };
            foreach (double[] p in sweep.Path)
            {
                payload.Path.Add((double[])p.Clone());
            }

            return new FeatureData { Kind = "sweep", Name = NameOf(sweep), Sweep = payload };
        }

        private static FeatureData TranslateLoft(InventorLoft loft)
        {
            var payload = new LoftData
            {
                Closed = loft.Closed ? true : (bool?)null,
                Operation = OperationName(loft.Operation),
            };
            foreach (InventorLoftSection s in loft.Sections)
            {
                payload.Sections.Add(new LoftSectionData { Sketch = s.SketchIndex, Profile = s.ProfileIndex });
            }

            return new FeatureData { Kind = "loft", Name = NameOf(loft), Loft = payload };
        }

        private FeatureData? TranslateRectPattern(
            InventorRectangularPattern pattern, IReadOnlyDictionary<int, int> sourceIndex)
        {
            if (!TryResolveSources(pattern, sourceIndex, out List<int> sources))
            {
                return null;
            }

            var payload = new RectPatternData
            {
                CountX = pattern.CountX,
                CountY = pattern.CountY,
                StepX = (double[])pattern.StepX.Clone(),
                StepY = (double[])pattern.StepY.Clone(),
            };
            AddRange(payload.Source, sources);
            return new FeatureData { Kind = "rectangular-pattern", Name = NameOf(pattern), RectangularPattern = payload };
        }

        private FeatureData? TranslateCircPattern(
            InventorCircularPattern pattern, IReadOnlyDictionary<int, int> sourceIndex)
        {
            if (!TryResolveSources(pattern, sourceIndex, out List<int> sources))
            {
                return null;
            }

            var payload = new CircPatternData
            {
                Count = pattern.Count,
                Angle = pattern.AngleRadians != 0 ? pattern.AngleRadians : FullCircle,
                AxisPoint = (double[])pattern.AxisPoint.Clone(),
                AxisDir = (double[])pattern.AxisDir.Clone(),
            };
            AddRange(payload.Source, sources);
            return new FeatureData { Kind = "circular-pattern", Name = NameOf(pattern), CircularPattern = payload };
        }

        private FeatureData? TranslateMirror(InventorMirror mirror, IReadOnlyDictionary<int, int> sourceIndex)
        {
            if (!TryResolveSources(mirror, sourceIndex, out List<int> sources))
            {
                return null;
            }

            var payload = new MirrorData
            {
                Origin = (double[])mirror.PlaneOrigin.Clone(),
                Normal = (double[])mirror.PlaneNormal.Clone(),
            };
            AddRange(payload.Source, sources);
            return new FeatureData { Kind = "mirror", Name = NameOf(mirror), Mirror = payload };
        }

        // Maps a replicating feature's IR source indices to recipe program indices. Fails
        // (reports + returns false) if any source was skipped, since the pattern can't bind.
        private bool TryResolveSources(
            InventorReplicatingFeature feature, IReadOnlyDictionary<int, int> sourceIndex, out List<int> resolved)
        {
            resolved = new List<int>(feature.SourceFeatureIndices.Count);
            foreach (int ir in feature.SourceFeatureIndices)
            {
                if (!sourceIndex.TryGetValue(ir, out int recipeIndex))
                {
                    _report.Skip(feature.GetType().Name, $"'{feature.Name}' references untranslated feature {ir}");
                    return false;
                }

                resolved.Add(recipeIndex);
            }

            return true;
        }

        private static void AddRange(IList<int> target, IEnumerable<int> values)
        {
            foreach (int v in values) target.Add(v);
        }

        private static string? NameOf(InventorFeature feature) => feature.Name.Length == 0 ? null : feature.Name;

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
