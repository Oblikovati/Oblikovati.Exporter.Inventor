// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Builds the bridge's geometric selectors from the IR's geometric descriptors. A hole/dress-up
    /// authored over the bridge selects its faces/edges by GEOMETRY (centroid+normal, midpoint+
    /// direction), not by a reference key: an external author cannot mint the host's lineage keys,
    /// and a key located at author time does not survive the feature's recompute — geometry rebinds
    /// every time (host schema fields placementFaceGeom / edgesGeom / facesGeom).
    /// </summary>
    internal static class GeomSelectors
    {
        /// <summary>A face selector {centroid, normal} from a face descriptor.</summary>
        public static Dictionary<string, object?> Face(InventorFaceDescriptor face) =>
            new Dictionary<string, object?> { ["centroid"] = face.Centroid, ["normal"] = face.Normal };

        /// <summary>An edge selector {midpoint, direction} from an edge descriptor.</summary>
        public static Dictionary<string, object?> Edge(InventorEdgeDescriptor edge) =>
            new Dictionary<string, object?> { ["midpoint"] = edge.Midpoint, ["direction"] = edge.Direction };

        /// <summary>The face-selector list for a set of face descriptors.</summary>
        public static List<Dictionary<string, object?>> Faces(IEnumerable<InventorFaceDescriptor> faces)
        {
            var list = new List<Dictionary<string, object?>>();
            foreach (InventorFaceDescriptor f in faces)
                list.Add(Face(f));
            return list;
        }

        /// <summary>The edge-selector list for a set of edge descriptors.</summary>
        public static List<Dictionary<string, object?>> Edges(IEnumerable<InventorEdgeDescriptor> edges)
        {
            var list = new List<Dictionary<string, object?>>();
            foreach (InventorEdgeDescriptor e in edges)
                list.Add(Edge(e));
            return list;
        }
    }
}
