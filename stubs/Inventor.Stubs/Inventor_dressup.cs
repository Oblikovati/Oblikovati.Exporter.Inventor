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
}
