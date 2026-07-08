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
    /// from their vertices; a closed circular edge (a bore/boss rim) from its circle centre+axis;
    /// planar faces from their vertices (centroid) and their Plane geometry (normal) — a non-planar
    /// face is skipped.
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
                    continue; // placement face must resolve to a planar body face
                }

                AddHoles(ir, h, placement);
            }
        }

        // One Oblikovati hole per drill centre this feature places (a sketch placement drills
        // several). The centre fixes the exact position; an unreadable centre falls back to the
        // face centroid (Center left null). When no centre resolves at all, a single centroid hole.
        private static void AddHoles(InventorDocument ir, HoleFeature h, InventorFaceDescriptor placement)
        {
            List<double[]> centers = HoleCenters(h.HoleCenterPoints);
            // Treat a non-distance extent with no positive depth as through-all: to-face/to-next
            // bores otherwise read Depth 0 and remove nothing.
            bool through = h.ExtentType == PartFeatureExtentEnum.kThroughAllExtent
                || (h.ExtentType != PartFeatureExtentEnum.kDistanceExtent && h.Depth <= 0);
            if (centers.Count == 0)
            {
                centers.Add(null!); // no resolvable centre -> one hole at the face centroid
            }

            double? diameterCm = BoreDiameterCm(h);
            if (diameterCm == null || diameterCm.Value <= 0)
            {
                return; // can't size the bore (e.g. an unresolved tapped hole) -> skip, don't crash
            }

            foreach (double[] center in centers)
            {
                ir.Features.Add(new InventorHole
                {
                    Name = h.Name,
                    PlacementFace = placement,
                    DiameterCm = diameterCm.Value,
                    DepthCm = h.Depth,
                    ThroughAll = through,
                    Center = center,
                });
            }
        }

        // The bore diameter that removes material (cm). A drilled/clearance hole exposes
        // HoleDiameter; a TAPPED hole leaves it null and the drilled bore is the tap-drill (minor)
        // diameter carried on TapInfo. Returns null when no diameter can be resolved.
        private static double? BoreDiameterCm(HoleFeature h)
        {
            Parameter drill = h.HoleDiameter;
            if (drill != null)
            {
                return drill._Value; // drilled/clearance: a length Parameter, already in cm
            }

            if (h.Tapped && h.TapInfo is HoleTapInfo tap)
            {
                return TapLengthCm(tap.TapDrillDiameter ?? tap.MinorDiameterMax, tap.Metric);
            }

            return null;
        }

        // A tapped hole's TapInfo dimensions come back either as a length Parameter (cm) or, more
        // often, as a bare double in the THREAD standard's unit (millimetres for a metric thread,
        // inches otherwise) — not database cm. Convert accordingly.
        private static double? TapLengthCm(object? value, bool metric) => value switch
        {
            Parameter p => p._Value,
            double d => metric ? d / 10.0 : d * 2.54,
            _ => (double?)null,
        };

        // The 3D model-space centres of a hole feature's centre points (any placement type).
        private static List<double[]> HoleCenters(ObjectCollection points)
        {
            var centers = new List<double[]>();
            for (int i = 1; i <= points.Count; i++)
            {
                double[]? p = CenterPoint(points[i]);
                if (p != null)
                {
                    centers.Add(p);
                }
            }

            return centers;
        }

        // A centre point's 3D position: a sketch point, work point or B-rep vertex.
        private static double[]? CenterPoint(object point)
        {
            switch (point)
            {
                case SketchPoint sketchPoint:
                    return P3(sketchPoint.Geometry3d);
                case WorkPoint workPoint:
                    return P3(workPoint.Point);
                case Vertex vertex:
                    return P3(vertex.Point);
                default:
                    return null;
            }
        }

        // The planar body face a hole drills into. Inventor exposes the face differently per
        // placement kind; only PointHolePlacement was read before, so sketch/linear/concentric
        // holes (the common cases on real parts) were dropped and their material never removed.
        // A placement whose entity is a WorkPlane (not a body Face) is skipped — the descriptor
        // needs a real face to bind to.
        private static InventorFaceDescriptor? PlacementFace(HolePlacementDefinition placement)
        {
            object? entity = placement switch
            {
                PointHolePlacementDefinition point => point.Direction,
                LinearHolePlacementDefinition linear => linear.Plane,
                ConcentricHolePlacementDefinition concentric => concentric.Plane,
                SketchHolePlacementDefinition sketch => SketchPlacementPlane(sketch),
                _ => null,
            };

            return entity is Face face ? FaceDescriptor(face) : null;
        }

        // A sketch-placed hole exposes only its centre points; the placement plane is the entity
        // the centre points' sketch is built on (a body Face when the sketch is on a face).
        private static object? SketchPlacementPlane(SketchHolePlacementDefinition placement)
        {
            ObjectCollection points = placement.HoleCenterPoints;
            if (points.Count >= 1 && points[1] is SketchPoint point && point.Parent is PlanarSketch sketch)
            {
                return sketch.PlanarEntity;
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

                    // Some constant-radius edge sets (e.g. face/loop-defined fillets) reject
                    // get_Edges with E_FAIL; skip that set rather than aborting the whole export.
                    EdgeCollection? edges = TryGetEdges(set);
                    if (edges != null)
                    {
                        AddEdges(fillet.Edges, edges);
                    }
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

        // Reads an edge set's Edges, tolerating the E_FAIL that Inventor raises for edge sets whose
        // membership it will not enumerate (returns null so the caller skips just that set).
        private static EdgeCollection? TryGetEdges(FilletConstantRadiusEdgeSet set)
        {
            try
            {
                return set.Edges;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return null;
            }
        }

        private static void AddEdges(IList<InventorEdgeDescriptor> target, EdgeCollection edges)
        {
            if (edges == null)
            {
                return; // some chamfer/fillet edge sets expose a null edge collection
            }

            for (int i = 1; i <= edges.Count; i++)
            {
                InventorEdgeDescriptor? d = EdgeDescriptor((Edge)edges[i]);
                if (d != null)
                {
                    target.Add(d);
                }
            }
        }

        // A straight edge is described by its endpoint chord (midpoint + direction). A closed
        // circular edge (a bore/boss rim) has no start/stop vertex, so it is described by its
        // circle centre + axis instead — the form the reader's closedCircleOf resolves; without
        // this a fillet/chamfer on a bore was silently dropped. Any other vertex-less edge (a full
        // ellipse/spline) still can't be named, so it is skipped.
        private static InventorEdgeDescriptor? EdgeDescriptor(Edge e)
        {
            Vertex startVertex = e.StartVertex;
            Vertex stopVertex = e.StopVertex;
            if (startVertex == null || stopVertex == null)
            {
                return CircularDescriptor(e);
            }

            double[] a = P3(startVertex.Point);
            double[] b = P3(stopVertex.Point);
            return new InventorEdgeDescriptor
            {
                Midpoint = new[] { (a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2 },
                Direction = Normalize(new[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] }),
            };
        }

        // The centre+axis descriptor of a closed circular edge. Edge.Geometry is a Circle for a
        // full-circle rim; its Center is the representative point and its Normal the (sign-agnostic)
        // axis. Returns null for a non-circular vertex-less edge or if Inventor won't yield the curve.
        private static InventorEdgeDescriptor? CircularDescriptor(Edge e)
        {
            Circle? circle;
            try
            {
                circle = e.Geometry as Circle;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return null;
            }

            if (circle == null)
            {
                return null;
            }

            return new InventorEdgeDescriptor { Midpoint = P3(circle.Center), Direction = V(circle.Normal) };
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

            // The reader binds a descriptor face by its OUTWARD normal; the plane's own normal
            // points either way, so flip it when the face runs opposite its surface (IsParamReversed).
            // Without this, a placement face whose plane normal is inward (e.g. a hole's start face)
            // matches the wrong end face and the bore never binds.
            double[] normal = V(plane.Normal);
            if (face.IsParamReversed)
            {
                normal = new[] { -normal[0], -normal[1], -normal[2] };
            }

            return new InventorFaceDescriptor
            {
                Centroid = n == 0 ? new double[] { 0, 0, 0 } : new[] { x / n, y / n, z / n },
                Normal = normal,
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
