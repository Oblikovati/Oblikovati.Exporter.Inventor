// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Inventor;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Inv
{
    /// <summary>
    /// Reads a part's dress-up features (fillet/chamfer/shell) into the IR as ADR-0040 geometric
    /// descriptors: each selected edge becomes a midpoint+direction, each removed face a
    /// centroid+normal, which Oblikovati binds to the recomputed body. Straight edges are read
    /// from their vertices; planar faces from their vertices (centroid) and their Plane geometry
    /// (normal) — a non-planar face is skipped. Hole and draft extraction are a later step.
    /// </summary>
    public static class DressUpExtractor
    {
        public static void Extract(PartFeatures features, InventorDocument ir)
        {
            ExtractFillets(features.FilletFeatures, ir);
            ExtractChamfers(features.ChamferFeatures, ir);
            ExtractShells(features.ShellFeatures, ir);
            ExtractDrafts(features.FaceDraftFeatures, ir);
            ExtractHoles(features.HoleFeatures, ir);
        }

        private static void ExtractDrafts(FaceDraftFeatures drafts, InventorDocument ir)
        {
            for (int i = 1; i <= drafts.Count; i++)
            {
                FaceDraftFeature d = drafts[i];
                double[]? pull = ResolveDirection(d._PullDirection);
                if (pull == null)
                {
                    continue; // unresolved pull direction -> skip rather than guess
                }

                var draft = new InventorDraft { Name = d.Name, AngleRadians = d._DraftAngle._Value, Pull = pull };
                FaceCollection faces = d._InputFaces;
                for (int j = 1; j <= faces.Count; j++)
                {
                    InventorFaceDescriptor? face = FaceDescriptor((Face)faces[j]);
                    if (face != null)
                    {
                        draft.Faces.Add(face);
                    }
                }

                if (draft.Faces.Count > 0)
                {
                    ir.Features.Add(draft);
                }
            }
        }

        private static void ExtractHoles(HoleFeatures holes, InventorDocument ir)
        {
            for (int i = 1; i <= holes.Count; i++)
            {
                HoleFeature h = holes[i];
                InventorFaceDescriptor? placement = PlacementFace(h.PlacementDefinition);
                if (placement == null)
                {
                    continue; // only point-on-planar-face placement is read for now
                }

                ir.Features.Add(new InventorHole
                {
                    Name = h.Name,
                    PlacementFace = placement,
                    DiameterCm = h.HoleDiameter._Value,
                    DepthCm = h.Depth,
                    ThroughAll = h.ExtentType == PartFeatureExtentEnum.kThroughAllExtent,
                });
            }
        }

        // The planar face a point-placed hole drills into (its Direction entity).
        private static InventorFaceDescriptor? PlacementFace(HolePlacementDefinition placement)
        {
            if (placement is PointHolePlacementDefinition point && point.Direction is Face face)
            {
                return FaceDescriptor(face);
            }

            return null;
        }

        // A unit direction from a planar face/work plane (its normal) or a work axis/edge.
        private static double[]? ResolveDirection(object entity)
        {
            switch (entity)
            {
                case Face face when face.Geometry is Plane plane:
                    return V(plane.Normal);
                case WorkPlane wp:
                    return V(wp.Plane.Normal);
                case WorkAxis axis:
                    return V(axis.Line.Direction);
                case Edge edge:
                    double[] a = P3(edge.StartVertex.Point), b = P3(edge.StopVertex.Point);
                    return Normalize(new[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] });
                default:
                    return null;
            }
        }

        private static void ExtractFillets(FilletFeatures fillets, InventorDocument ir)
        {
            for (int i = 1; i <= fillets.Count; i++)
            {
                FilletFeature f = fillets[i];
                FilletDefinition def = f.FilletDefinition;
                var fillet = new InventorFillet { Name = f.Name };
                bool haveRadius = false;
                for (int s = 1; s <= def.EdgeSetCount; s++)
                {
                    if (!(def.get_EdgeSetItem(s) is FilletConstantRadiusEdgeSet set))
                    {
                        continue; // variable-radius / face sets are a later step
                    }

                    if (!haveRadius)
                    {
                        fillet.RadiusCm = set.Radius._Value;
                        haveRadius = true;
                    }

                    AddEdges(fillet.Edges, set.Edges);
                }

                if (fillet.Edges.Count > 0)
                {
                    ir.Features.Add(fillet);
                }
            }
        }

        private static void ExtractChamfers(ChamferFeatures chamfers, InventorDocument ir)
        {
            for (int i = 1; i <= chamfers.Count; i++)
            {
                ChamferFeature c = chamfers[i];
                var chamfer = new InventorChamfer { Name = c.Name, DistanceCm = c.Definition.Distance._Value };
                AddEdges(chamfer.Edges, c.ChamferedEdges);
                if (chamfer.Edges.Count > 0)
                {
                    ir.Features.Add(chamfer);
                }
            }
        }

        private static void ExtractShells(ShellFeatures shells, InventorDocument ir)
        {
            for (int i = 1; i <= shells.Count; i++)
            {
                ShellFeature s = shells[i];
                ShellDefinition def = s.Definition;
                var shell = new InventorShell { Name = s.Name, ThicknessCm = def.Thickness._Value };
                FaceCollection faces = def.InputFaces;
                for (int j = 1; j <= faces.Count; j++)
                {
                    InventorFaceDescriptor? face = FaceDescriptor((Face)faces[j]);
                    if (face != null)
                    {
                        shell.RemovedFaces.Add(face);
                    }
                }

                if (shell.RemovedFaces.Count > 0)
                {
                    ir.Features.Add(shell);
                }
            }
        }

        private static void AddEdges(IList<InventorEdgeDescriptor> target, EdgeCollection edges)
        {
            for (int i = 1; i <= edges.Count; i++)
            {
                var e = (Edge)edges[i];
                double[] a = P3(e.StartVertex.Point);
                double[] b = P3(e.StopVertex.Point);
                target.Add(new InventorEdgeDescriptor
                {
                    Midpoint = new[] { (a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2 },
                    Direction = Normalize(new[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] }),
                });
            }
        }

        // A planar face's centroid (its vertices' average) and outward normal (its Plane). Returns
        // null for a non-planar face (no Plane geometry) — handled by a later step.
        private static InventorFaceDescriptor? FaceDescriptor(Face face)
        {
            if (!(face.Geometry is Plane plane))
            {
                return null;
            }

            Vertices vertices = face.Vertices;
            double x = 0, y = 0, z = 0;
            int n = vertices.Count;
            for (int i = 1; i <= n; i++)
            {
                double[] p = P3(vertices[i].Point);
                x += p[0];
                y += p[1];
                z += p[2];
            }

            return new InventorFaceDescriptor
            {
                Centroid = n == 0 ? new double[] { 0, 0, 0 } : new[] { x / n, y / n, z / n },
                Normal = V(plane.Normal),
            };
        }

        private static double[] P3(Point p) => new[] { p.X, p.Y, p.Z };

        private static double[] V(UnitVector v) => new[] { v.X, v.Y, v.Z };

        private static double[] Normalize(double[] a)
        {
            double len = Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]);
            return len == 0 ? new double[] { 0, 0, 0 } : new[] { a[0] / len, a[1] / len, a[2] / len };
        }
    }
}
