// SPDX-License-Identifier: GPL-2.0-only
using System;
using Inventor;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Inv
{
    /// <summary>
    /// Reads a feature's SELECTED profile (Inventor <c>Profile.ProfilePaths</c>) into the IR as
    /// explicit loops. Inventor's profile already excludes spurious/projected reference geometry
    /// and includes exactly the reference geometry that IS the boundary, so authoring these loops
    /// lets the emitter reproduce the exact profile Inventor used rather than the whole shared
    /// sketch plus a seed point. Each entity is authored by its own intrinsic geometry (the
    /// per-entity <see cref="SketchExtractor"/> builders); loop closure comes from shared
    /// endpoints, so the entity's OpposedToSketchEntity direction is not applied.
    /// </summary>
    internal static class ProfileExtractor
    {
        internal static void Extract(ExtrudeFeature ext, InventorExtrude ir) =>
            ExtractInto(ext.Profile, ir.ProfileLoops);

        internal static void Extract(RevolveFeature rev, InventorRevolve ir) =>
            ExtractInto(rev.Profile, ir.ProfileLoops);

        private static void ExtractInto(Profile profile, System.Collections.Generic.IList<InventorProfileLoop> loops)
        {
            try
            {
                foreach (ProfilePath path in profile)
                {
                    var loop = new InventorProfileLoop
                    {
                        // Inventor's AddsMaterial=false path is an inner (hole) loop.
                        IsHole = !path.AddsMaterial,
                    };
                    foreach (ProfileEntity pe in path)
                    {
                        InventorCurve? c = BuildCurve(pe.SketchEntity);
                        if (c != null)
                        {
                            loop.Curves.Add(c);
                        }
                    }

                    if (loop.Curves.Count >= 1)
                    {
                        loops.Add(loop);
                    }
                }
            }
            catch (Exception)
            {
                // The profile could not be read faithfully — leave ProfileLoops empty so the emitter
                // falls back to the shared sketch + interior seed points. Never throw.
                loops.Clear();
            }
        }

        // Maps one profile entity's sketch entity to an IR curve via the per-entity builders. Real
        // interop resolves the concrete type via QueryInterface; the flat item is typed object.
        private static InventorCurve? BuildCurve(object sketchEntity)
        {
            if (sketchEntity is SketchLine l)
            {
                return SketchExtractor.BuildLineCurve(l);
            }

            if (sketchEntity is SketchCircle circle)
            {
                return SketchExtractor.BuildCircleCurve(circle);
            }

            if (sketchEntity is SketchArc arc)
            {
                return SketchExtractor.BuildArcCurve(arc);
            }

            if (sketchEntity is SketchSpline spline)
            {
                return SketchExtractor.BuildSplineCurve(spline);
            }

            if (sketchEntity is SketchControlPointSpline cpSpline)
            {
                return SketchExtractor.BuildControlPointSplineCurve(cpSpline);
            }

            if (sketchEntity is SketchEllipse ellipse)
            {
                return SketchExtractor.BuildEllipseCurve(ellipse);
            }

            if (sketchEntity is SketchEllipticalArc ellipticalArc)
            {
                return SketchExtractor.BuildEllipticalArcCurve(ellipticalArc);
            }

            // null or an unhandled sketch-entity type: skip it (the loop still closes on the rest).
            return null;
        }
    }
}
