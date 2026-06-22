// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.Inventor.Model
{
    /// <summary>
    /// A geometric edge descriptor: the edge's midpoint and direction in model space (cm). The
    /// adapter computes it from an Inventor edge; Oblikovati binds it to a body edge on recompute
    /// (ADR-0040) — an external author cannot mint Oblikovati lineage keys, so it ships geometry.
    /// </summary>
    public sealed class InventorEdgeDescriptor
    {
        public double[] Midpoint { get; set; } = { 0, 0, 0 };

        public double[] Direction { get; set; } = { 0, 0, 0 };
    }

    /// <summary>A geometric face descriptor: centroid + outward normal (cm / unit).</summary>
    public sealed class InventorFaceDescriptor
    {
        public double[] Centroid { get; set; } = { 0, 0, 0 };

        public double[] Normal { get; set; } = { 0, 0, 1 };
    }

    /// <summary>A fillet rounding the given edges (geometric descriptors) to RadiusCm.</summary>
    public sealed class InventorFillet : InventorFeature
    {
        public IList<InventorEdgeDescriptor> Edges { get; } = new List<InventorEdgeDescriptor>();

        public double RadiusCm { get; set; }
    }

    /// <summary>A chamfer bevelling the given edges by DistanceCm (equal distance).</summary>
    public sealed class InventorChamfer : InventorFeature
    {
        public IList<InventorEdgeDescriptor> Edges { get; } = new List<InventorEdgeDescriptor>();

        public double DistanceCm { get; set; }
    }

    /// <summary>A shell hollowing the body, removing the given faces, to ThicknessCm.</summary>
    public sealed class InventorShell : InventorFeature
    {
        public IList<InventorFaceDescriptor> RemovedFaces { get; } = new List<InventorFaceDescriptor>();

        public double ThicknessCm { get; set; }
    }

    /// <summary>A draft tapering the given faces by AngleRadians about a pull direction.</summary>
    public sealed class InventorDraft : InventorFeature
    {
        public IList<InventorFaceDescriptor> Faces { get; } = new List<InventorFaceDescriptor>();

        public double AngleRadians { get; set; }

        /// <summary>Pull direction (unit); defaults to +Z.</summary>
        public double[] Pull { get; set; } = { 0, 0, 1 };
    }

    /// <summary>A drilled hole on a placement face (geometric descriptor). Lengths cm.</summary>
    public sealed class InventorHole : InventorFeature
    {
        public InventorFaceDescriptor PlacementFace { get; set; } = new InventorFaceDescriptor();

        public double DiameterCm { get; set; }

        public double DepthCm { get; set; }

        public bool ThroughAll { get; set; }
    }
}
