// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.Inventor.Model
{
    /// <summary>
    /// A sweep of a sketch profile along a path. The profile is a sketch region; the path is an
    /// explicit polyline of 3D points in model space (cm) — Oblikovati stores the path as points,
    /// not a sketch reference.
    /// </summary>
    public sealed class InventorSweep : InventorFeature
    {
        public int ProfileSketchIndex { get; set; }

        public int ProfileIndex { get; set; }

        /// <summary>The sweep path as a 3D polyline (cm).</summary>
        public IList<double[]> Path { get; } = new List<double[]>();

        public bool Closed { get; set; }

        public InventorOperation Operation { get; set; } = InventorOperation.NewBody;
    }

    /// <summary>One section of a loft: a profile (a region of a sketch).</summary>
    public sealed class InventorLoftSection
    {
        public int SketchIndex { get; set; }

        public int ProfileIndex { get; set; }
    }

    /// <summary>A loft through an ordered list of profile sections.</summary>
    public sealed class InventorLoft : InventorFeature
    {
        public IList<InventorLoftSection> Sections { get; } = new List<InventorLoftSection>();

        public bool Closed { get; set; }

        public InventorOperation Operation { get; set; } = InventorOperation.NewBody;
    }
}
