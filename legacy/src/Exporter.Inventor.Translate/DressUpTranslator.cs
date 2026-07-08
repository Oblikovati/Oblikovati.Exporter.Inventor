// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>
    /// Translates Inventor dress-up features (fillet/chamfer/shell/draft/hole) into recipe
    /// features whose edge/face selections are GEOMETRIC descriptors (ADR-0040) — the path that
    /// lets the exporter place them without Oblikovati lineage keys; the reader binds each to a
    /// body edge/face on recompute. Coordinates pass through (cm); the draft angle is radians.
    /// </summary>
    public static class DressUpTranslator
    {
        public static FeatureData Fillet(InventorFillet fillet)
        {
            var payload = new EdgeDressData { Value = fillet.RadiusCm };
            AddEdges(payload, fillet.Edges);
            return new FeatureData { Kind = "fillet", Name = NameOf(fillet), Fillet = payload };
        }

        public static FeatureData Chamfer(InventorChamfer chamfer)
        {
            var payload = new EdgeDressData { Value = chamfer.DistanceCm };
            AddEdges(payload, chamfer.Edges);
            return new FeatureData { Kind = "chamfer", Name = NameOf(chamfer), Chamfer = payload };
        }

        public static FeatureData Shell(InventorShell shell)
        {
            var payload = new FaceDressData { Value = shell.ThicknessCm };
            AddFaces(payload, shell.RemovedFaces);
            return new FeatureData { Kind = "shell", Name = NameOf(shell), Shell = payload };
        }

        public static FeatureData Draft(InventorDraft draft)
        {
            var payload = new FaceDressData { Value = draft.AngleRadians, Pull = (double[])draft.Pull.Clone() };
            AddFaces(payload, draft.Faces);
            return new FeatureData { Kind = "draft", Name = NameOf(draft), Draft = payload };
        }

        public static FeatureData Hole(InventorHole hole)
        {
            var payload = new HoleData
            {
                Diameter = hole.DiameterCm,
                Depth = hole.DepthCm,
                ThroughAll = hole.ThroughAll ? true : (bool?)null,
                Type = "drilled",
                GeomFace = FaceRef(hole.PlacementFace),
                Center = hole.Center == null ? null : (double[])hole.Center.Clone(),
            };
            return new FeatureData { Kind = "hole", Name = NameOf(hole), Hole = payload };
        }

        private static void AddEdges(EdgeDressData payload, IEnumerable<InventorEdgeDescriptor> edges)
        {
            foreach (InventorEdgeDescriptor e in edges)
            {
                payload.GeomEdges.Add(new GeomEdgeRefData
                {
                    Midpoint = (double[])e.Midpoint.Clone(),
                    Direction = (double[])e.Direction.Clone(),
                });
            }
        }

        private static void AddFaces(FaceDressData payload, IEnumerable<InventorFaceDescriptor> faces)
        {
            foreach (InventorFaceDescriptor f in faces)
            {
                payload.GeomFaces.Add(FaceRef(f));
            }
        }

        private static GeomFaceRefData FaceRef(InventorFaceDescriptor f) =>
            new GeomFaceRefData { Centroid = (double[])f.Centroid.Clone(), Normal = (double[])f.Normal.Clone() };

        private static string? NameOf(InventorFeature feature) => feature.Name.Length == 0 ? null : feature.Name;
    }
}
