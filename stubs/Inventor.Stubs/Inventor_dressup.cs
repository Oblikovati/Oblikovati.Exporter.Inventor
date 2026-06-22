// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor dress-up read surface (fillet/chamfer/shell) the adapter
// walks. Shapes mirror the genuine Autodesk.Inventor.Interop; edge/face collections are 1-based
// and object-indexed (cast to Edge/Face). Edge midpoint+direction come from the vertices; a face
// centroid+normal from its vertices and its planar Geometry.
namespace Inventor
{
    /// <summary>Stub of an edge collection (a dress-up's selected edges); items are Edge.</summary>
    public class EdgeCollection
    {
        public virtual int Count => throw Stub.Error();

        public virtual object this[int index] => throw Stub.Error();
    }

    /// <summary>Stub of a face collection (a dress-up's selected faces); items are Face.</summary>
    public class FaceCollection
    {
        public virtual int Count => throw Stub.Error();

        public virtual object this[int index] => throw Stub.Error();
    }

    /// <summary>Stub of a vertices collection (a face's corners).</summary>
    public class Vertices
    {
        public virtual int Count => throw Stub.Error();

        public virtual Vertex this[int index] => throw Stub.Error();
    }

    /// <summary>Stub of the fillet-features collection (object-indexed, 1-based).</summary>
    public class FilletFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual FilletFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one fillet feature; its definition holds the edge sets and radii.</summary>
    public class FilletFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual FilletDefinition FilletDefinition => throw Stub.Error();
    }

    /// <summary>
    /// Stub of a fillet definition: a 1-based collection of edge sets read via EdgeSetCount and
    /// the get_EdgeSetItem accessor (the real Item is a parameterized COM property). Each item is
    /// a FilletRadiusEdgeSet (cast to FilletConstantRadiusEdgeSet for a constant-radius fillet).
    /// </summary>
    public class FilletDefinition
    {
        public virtual int EdgeSetCount => throw Stub.Error();

        public virtual object get_EdgeSetItem(int index) => throw Stub.Error();
    }

    /// <summary>Stub of a constant-radius edge set: the edges and their shared radius.</summary>
    public class FilletConstantRadiusEdgeSet
    {
        public virtual EdgeCollection Edges => throw Stub.Error();

        public virtual Parameter Radius => throw Stub.Error();
    }

    /// <summary>Stub of the chamfer-features collection (object-indexed, 1-based).</summary>
    public class ChamferFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual ChamferFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one chamfer feature; the chamfered edges and the distance definition.</summary>
    public class ChamferFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual EdgeCollection ChamferedEdges => throw Stub.Error();

        public virtual ChamferDefinition Definition => throw Stub.Error();
    }

    /// <summary>Stub of a chamfer definition (Distance applies to the equal-distance type).</summary>
    public class ChamferDefinition
    {
        public virtual Parameter Distance => throw Stub.Error();
    }

    /// <summary>Stub of the shell-features collection (object-indexed, 1-based).</summary>
    public class ShellFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual ShellFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one shell feature; its definition holds the removed faces and thickness.</summary>
    public class ShellFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual ShellDefinition Definition => throw Stub.Error();
    }

    /// <summary>Stub of a shell definition: the removed (input) faces and the wall thickness.</summary>
    public class ShellDefinition
    {
        public virtual FaceCollection InputFaces => throw Stub.Error();

        public virtual Parameter Thickness => throw Stub.Error();
    }

    /// <summary>Stub of the face-draft features collection (object-indexed, 1-based).</summary>
    public class FaceDraftFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual FaceDraftFeature this[object index] => throw Stub.Error();
    }

    /// <summary>
    /// Stub of one face-draft feature. The underscore accessors are the strongly-typed COM
    /// variants of the definition's object-typed members. PullDirection is a planar face/work
    /// plane (its normal) or an axis/edge (its direction).
    /// </summary>
    public class FaceDraftFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual Parameter _DraftAngle => throw Stub.Error();

        public virtual FaceCollection _InputFaces => throw Stub.Error();

        public virtual object _PullDirection => throw Stub.Error();
    }

    /// <summary>Stub of the hole features collection (object-indexed, 1-based).</summary>
    public class HoleFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual HoleFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one hole feature; diameter/depth/extent plus its placement.</summary>
    public class HoleFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual Parameter HoleDiameter => throw Stub.Error();

        public virtual double Depth => throw Stub.Error();

        public virtual PartFeatureExtentEnum ExtentType => throw Stub.Error();

        public virtual HolePlacementDefinition PlacementDefinition => throw Stub.Error();

        /// <summary>The drill centre points of every hole this feature places (any placement type).</summary>
        public virtual ObjectCollection HoleCenterPoints => throw Stub.Error();
    }

    /// <summary>Stub of a work point; Point is its model-space 3D position.</summary>
    public class WorkPoint
    {
        public virtual Point Point => throw Stub.Error();
    }

    /// <summary>Base of the hole-placement definitions (the concrete kind keys the placement face).</summary>
    public class HolePlacementDefinition
    {
    }

    /// <summary>
    /// Stub of a point-based hole placement. Direction is the entity defining the hole axis —
    /// a planar Face (or WorkPlane) the hole drills into, used as the placement face.
    /// </summary>
    public class PointHolePlacementDefinition : HolePlacementDefinition
    {
        public virtual object Direction => throw Stub.Error();
    }
}
