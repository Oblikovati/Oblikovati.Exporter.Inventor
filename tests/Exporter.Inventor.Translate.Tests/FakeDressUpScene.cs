// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using System.Linq;
using Inventor;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Named fakes of the Inventor dress-up surface (fillet/chamfer/shell), subclassing the stub
    /// so the real DressUpExtractor runs with no Inventor install.
    /// </summary>
    public sealed class FakeFilletFeatures : FilletFeatures
    {
        private readonly IList<FilletFeature> _items;
        public FakeFilletFeatures(IList<FilletFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override FilletFeature this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeFilletFeature : FilletFeature
    {
        private readonly string _name;
        private readonly FilletDefinition _def;
        public FakeFilletFeature(string name, double radius, IList<Edge> edges)
        {
            _name = name;
            _def = new FakeFilletDefinition(new FakeFilletConstantRadiusEdgeSet(radius, edges));
        }
        public override string Name => _name;
        public override FilletDefinition FilletDefinition => _def;
    }

    public sealed class FakeFilletDefinition : FilletDefinition
    {
        private readonly object _set;
        public FakeFilletDefinition(object set) => _set = set;
        public override int EdgeSetCount => 1;
        public override object get_EdgeSetItem(int index) => _set;
    }

    public sealed class FakeFilletConstantRadiusEdgeSet : FilletConstantRadiusEdgeSet
    {
        private readonly EdgeCollection _edges;
        private readonly double _radius;
        public FakeFilletConstantRadiusEdgeSet(double radius, IList<Edge> edges)
        {
            _radius = radius;
            _edges = new FakeEdgeCollection(edges);
        }
        public override EdgeCollection Edges => _edges;
        public override Parameter Radius => new FakeDistanceParameter(_radius);
    }

    public sealed class FakeChamferFeatures : ChamferFeatures
    {
        private readonly IList<ChamferFeature> _items;
        public FakeChamferFeatures(IList<ChamferFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override ChamferFeature this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeChamferFeature : ChamferFeature
    {
        private readonly string _name;
        private readonly EdgeCollection _edges;
        private readonly ChamferDefinition _def;
        public FakeChamferFeature(string name, double distance, IList<Edge> edges)
        {
            _name = name;
            _edges = new FakeEdgeCollection(edges);
            _def = new FakeChamferDefinition(distance);
        }
        public override string Name => _name;
        public override EdgeCollection ChamferedEdges => _edges;
        public override ChamferDefinition Definition => _def;
    }

    public sealed class FakeChamferDefinition : ChamferDefinition
    {
        private readonly double _distance;
        public FakeChamferDefinition(double distance) => _distance = distance;
        public override Parameter Distance => new FakeDistanceParameter(_distance);
    }

    public sealed class FakeShellFeatures : ShellFeatures
    {
        private readonly IList<ShellFeature> _items;
        public FakeShellFeatures(IList<ShellFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override ShellFeature this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeShellFeature : ShellFeature
    {
        private readonly string _name;
        private readonly ShellDefinition _def;
        public FakeShellFeature(string name, double thickness, IList<Face> faces)
        {
            _name = name;
            _def = new FakeShellDefinition(thickness, faces);
        }
        public override string Name => _name;
        public override ShellDefinition Definition => _def;
    }

    public sealed class FakeShellDefinition : ShellDefinition
    {
        private readonly FaceCollection _faces;
        private readonly double _thickness;
        public FakeShellDefinition(double thickness, IList<Face> faces)
        {
            _thickness = thickness;
            _faces = new FakeFaceCollection(faces);
        }
        public override FaceCollection InputFaces => _faces;
        public override Parameter Thickness => new FakeDistanceParameter(_thickness);
    }

    public sealed class FakeEdgeCollection : EdgeCollection
    {
        private readonly IList<Edge> _items;
        public FakeEdgeCollection(IList<Edge> items) => _items = items;
        public override int Count => _items.Count;
        public override object this[int index] => _items[index - 1];
    }

    public sealed class FakeFaceCollection : FaceCollection
    {
        private readonly IList<Face> _items;
        public FakeFaceCollection(IList<Face> items) => _items = items;
        public override int Count => _items.Count;
        public override object this[int index] => _items[index - 1];
    }

    /// <summary>A straight B-rep edge between two points.</summary>
    public sealed class FakeBrepEdge : Edge
    {
        private readonly Vertex _start;
        private readonly Vertex _stop;
        public FakeBrepEdge(double[] a, double[] b)
        {
            _start = new FakeBrepVertex(a);
            _stop = new FakeBrepVertex(b);
        }
        public override Vertex StartVertex => _start;
        public override Vertex StopVertex => _stop;
    }

    public sealed class FakeBrepVertex : Vertex
    {
        private readonly Point _point;
        public FakeBrepVertex(double[] p) => _point = new FakePoint(p[0], p[1], p[2]);
        public override Point Point => _point;
    }

    /// <summary>A planar B-rep face: corner vertices (centroid) + a plane normal.</summary>
    public sealed class FakePlanarFace : Face
    {
        private readonly Plane _plane;
        private readonly Vertices _vertices;
        public FakePlanarFace(double[][] corners, double[] normal)
        {
            _plane = new FakeWorkPlaneGeometry(corners.Length > 0 ? corners[0] : new double[] { 0, 0, 0 }, normal);
            var verts = new List<Vertex>();
            foreach (double[] c in corners) verts.Add(new FakeBrepVertex(c));
            _vertices = new FakeVertices(verts);
        }
        public override object Geometry => _plane;
        public override Vertices Vertices => _vertices;
    }

    public sealed class FakeVertices : Vertices
    {
        private readonly IList<Vertex> _items;
        public FakeVertices(IList<Vertex> items) => _items = items;
        public override int Count => _items.Count;
        public override Vertex this[int index] => _items[index - 1];
    }

    public sealed class FakeFaceDraftFeatures : FaceDraftFeatures
    {
        private readonly IList<FaceDraftFeature> _items;
        public FakeFaceDraftFeatures(IList<FaceDraftFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override FaceDraftFeature this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeFaceDraftFeature : FaceDraftFeature
    {
        private readonly string _name;
        private readonly double _angle;
        private readonly FaceCollection _faces;
        private readonly object _pull;
        public FakeFaceDraftFeature(string name, double angle, IList<Face> faces, object pull)
        {
            _name = name;
            _angle = angle;
            _faces = new FakeFaceCollection(faces);
            _pull = pull;
        }
        public override string Name => _name;
        public override Parameter _DraftAngle => new FakeDistanceParameter(_angle);
        public override FaceCollection _InputFaces => _faces;
        public override object _PullDirection => _pull;
    }

    public sealed class FakeHoleFeatures : HoleFeatures
    {
        private readonly IList<HoleFeature> _items;
        public FakeHoleFeatures(IList<HoleFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override HoleFeature this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeHoleFeature : HoleFeature
    {
        private readonly string _name;
        private readonly double _diameter;
        private readonly double _depth;
        private readonly bool _throughAll;
        private readonly HolePlacementDefinition _placement;
        private readonly ObjectCollection _centers;
        public FakeHoleFeature(string name, double diameter, double depth, bool throughAll, Face placementFace, double[][]? centers = null)
        {
            _name = name;
            _diameter = diameter;
            _depth = depth;
            _throughAll = throughAll;
            _placement = new FakePointHolePlacementDefinition(placementFace);
            object[] points = (centers ?? System.Array.Empty<double[]>())
                .Select(c => (object)new FakeHoleCenterPoint(c[0], c[1], c[2])).ToArray();
            _centers = new FakeObjectList(points);
        }
        public override string Name => _name;
        public override Parameter HoleDiameter => new FakeDistanceParameter(_diameter);
        public override double Depth => _depth;
        public override PartFeatureExtentEnum ExtentType =>
            _throughAll ? PartFeatureExtentEnum.kThroughAllExtent : PartFeatureExtentEnum.kDistanceExtent;
        public override HolePlacementDefinition PlacementDefinition => _placement;
        public override ObjectCollection HoleCenterPoints => _centers;
    }

    /// <summary>A hole-centre sketch point that carries its model-space 3D position.</summary>
    public sealed class FakeHoleCenterPoint : SketchPoint
    {
        private readonly Point _point;
        public FakeHoleCenterPoint(double x, double y, double z) => _point = new FakePoint(x, y, z);
        public override Point Geometry3d => _point;
    }

    public sealed class FakePointHolePlacementDefinition : PointHolePlacementDefinition
    {
        private readonly object _direction;
        public FakePointHolePlacementDefinition(object direction) => _direction = direction;
        public override object Direction => _direction;
    }
}
