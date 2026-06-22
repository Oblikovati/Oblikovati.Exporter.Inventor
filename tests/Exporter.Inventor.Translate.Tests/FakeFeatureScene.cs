// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Named fakes of the Inventor feature + work-plane surface, subclassing the all-virtual
    /// stub so the real <c>FeatureExtractor</c> runs with no Inventor install.
    /// </summary>
    public sealed class FakePartFeatures : PartFeatures
    {
        private readonly ExtrudeFeatures _extrudes;
        private readonly RevolveFeatures _revolves;
        private readonly RectangularPatternFeatures _rectPatterns;
        private readonly CircularPatternFeatures _circPatterns;
        private readonly MirrorFeatures _mirrors;
        private readonly FilletFeatures _fillets;
        private readonly ChamferFeatures _chamfers;
        private readonly ShellFeatures _shells;
        private readonly FaceDraftFeatures _drafts;
        private readonly HoleFeatures _holes;
        private readonly LoftFeatures _lofts;
        private readonly SweepFeatures _sweeps;

        public FakePartFeatures(
            IList<ExtrudeFeature> extrudes,
            IList<RevolveFeature>? revolves = null,
            IList<RectangularPatternFeature>? rectPatterns = null,
            IList<CircularPatternFeature>? circPatterns = null,
            IList<MirrorFeature>? mirrors = null,
            IList<FilletFeature>? fillets = null,
            IList<ChamferFeature>? chamfers = null,
            IList<ShellFeature>? shells = null,
            IList<FaceDraftFeature>? drafts = null,
            IList<HoleFeature>? holes = null,
            IList<LoftFeature>? lofts = null,
            IList<SweepFeature>? sweeps = null)
        {
            _extrudes = new FakeExtrudeFeatures(extrudes);
            _revolves = new FakeRevolveFeatures(revolves ?? new List<RevolveFeature>());
            _rectPatterns = new FakeRectangularPatternFeatures(rectPatterns ?? new List<RectangularPatternFeature>());
            _circPatterns = new FakeCircularPatternFeatures(circPatterns ?? new List<CircularPatternFeature>());
            _mirrors = new FakeMirrorFeatures(mirrors ?? new List<MirrorFeature>());
            _fillets = new FakeFilletFeatures(fillets ?? new List<FilletFeature>());
            _chamfers = new FakeChamferFeatures(chamfers ?? new List<ChamferFeature>());
            _shells = new FakeShellFeatures(shells ?? new List<ShellFeature>());
            _drafts = new FakeFaceDraftFeatures(drafts ?? new List<FaceDraftFeature>());
            _holes = new FakeHoleFeatures(holes ?? new List<HoleFeature>());
            _lofts = new FakeLoftFeatures(lofts ?? new List<LoftFeature>());
            _sweeps = new FakeSweepFeatures(sweeps ?? new List<SweepFeature>());
        }

        public override ExtrudeFeatures ExtrudeFeatures => _extrudes;

        public override RevolveFeatures RevolveFeatures => _revolves;

        public override RectangularPatternFeatures RectangularPatternFeatures => _rectPatterns;

        public override CircularPatternFeatures CircularPatternFeatures => _circPatterns;

        public override MirrorFeatures MirrorFeatures => _mirrors;

        public override FilletFeatures FilletFeatures => _fillets;

        public override ChamferFeatures ChamferFeatures => _chamfers;

        public override ShellFeatures ShellFeatures => _shells;

        public override FaceDraftFeatures FaceDraftFeatures => _drafts;

        public override HoleFeatures HoleFeatures => _holes;

        public override LoftFeatures LoftFeatures => _lofts;

        public override SweepFeatures SweepFeatures => _sweeps;
    }

    public sealed class FakeSweepFeatures : SweepFeatures
    {
        private readonly IList<SweepFeature> _items;
        public FakeSweepFeatures(IList<SweepFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override SweepFeature this[object index] => _items[(int)index - 1];
    }

    /// <summary>A sweep of a profile on a named sketch along a straight-segment path (3D points).</summary>
    public sealed class FakeSweepFeature : SweepFeature
    {
        private readonly string _name;
        private readonly Profile _profile;
        private readonly Path _path;
        public FakeSweepFeature(string name, string profileSketchName, double[][] pathPoints)
        {
            _name = name;
            _profile = new FakeProfile(profileSketchName);
            _path = new FakePath(pathPoints);
        }
        public override string Name => _name;
        public override PartFeatureOperationEnum Operation => PartFeatureOperationEnum.kNewBodyOperation;
        public override Profile Profile => _profile;
        public override Path Path => _path;
    }

    /// <summary>A path of straight segments built from a point polyline.</summary>
    public sealed class FakePath : Path
    {
        private readonly IList<PathEntity> _entities;
        public FakePath(double[][] points)
        {
            _entities = new List<PathEntity>();
            for (int i = 0; i + 1 < points.Length; i++)
            {
                _entities.Add(new FakePathEntity(points[i], points[i + 1]));
            }
        }
        public override int Count => _entities.Count;
        public override PathEntity this[int index] => _entities[index - 1];
    }

    public sealed class FakePathEntity : PathEntity
    {
        private readonly SketchLine _line;
        public FakePathEntity(double[] a, double[] b) => _line = new FakePathLine(a, b);
        public override object SketchEntity => _line;
        public override bool OpposedToSketchEntity => false;
    }

    /// <summary>A sketch line that only needs its 3D geometry (for a sweep path segment).</summary>
    public sealed class FakePathLine : SketchLine
    {
        private readonly LineSegment _geometry;
        public FakePathLine(double[] a, double[] b) => _geometry = new FakeLineSegment(a, b);
        public override LineSegment Geometry3d => _geometry;
    }

    public sealed class FakeLineSegment : LineSegment
    {
        private readonly Point _start;
        private readonly Point _end;
        public FakeLineSegment(double[] a, double[] b)
        {
            _start = new FakePoint(a[0], a[1], a[2]);
            _end = new FakePoint(b[0], b[1], b[2]);
        }
        public override Point StartPoint => _start;
        public override Point EndPoint => _end;
    }

    public sealed class FakeLoftFeatures : LoftFeatures
    {
        private readonly IList<LoftFeature> _items;
        public FakeLoftFeatures(IList<LoftFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override LoftFeature this[object index] => _items[(int)index - 1];
    }

    /// <summary>A loft whose sections are profiles on the named sketches.</summary>
    public sealed class FakeLoftFeature : LoftFeature
    {
        private readonly string _name;
        private readonly ObjectCollection _sections;
        public FakeLoftFeature(string name, params string[] sectionSketchNames)
        {
            _name = name;
            var items = new object[sectionSketchNames.Length];
            for (int i = 0; i < sectionSketchNames.Length; i++) items[i] = new FakeProfile(sectionSketchNames[i]);
            _sections = new FakeObjectList(items);
        }
        public override string Name => _name;
        public override PartFeatureOperationEnum Operation => PartFeatureOperationEnum.kNewBodyOperation;
        public override ObjectCollection Sections => _sections;
    }

    /// <summary>An object collection over arbitrary items (e.g. loft section profiles).</summary>
    public sealed class FakeObjectList : ObjectCollection
    {
        private readonly object[] _items;
        public FakeObjectList(object[] items) => _items = items;
        public override int Count => _items.Length;
        public override object this[int index] => _items[index - 1];
    }

    public sealed class FakeRevolveFeatures : RevolveFeatures
    {
        private readonly IList<RevolveFeature> _items;

        public FakeRevolveFeatures(IList<RevolveFeature> items)
        {
            _items = items;
        }

        public override int Count => _items.Count;

        public override RevolveFeature this[object index] => _items[(int)index - 1];
    }

    /// <summary>A full-revolution revolve fake: axis line + parent sketch name + operation.</summary>
    public sealed class FakeRevolveFeature : RevolveFeature
    {
        private readonly string _name;
        private readonly PartFeatureOperationEnum _operation;
        private readonly Profile _profile;
        private readonly SketchLine _axis;

        public FakeRevolveFeature(
            string name, string parentSketchName, SketchLine axis,
            PartFeatureOperationEnum operation = PartFeatureOperationEnum.kNewBodyOperation)
        {
            _name = name;
            _operation = operation;
            _profile = new FakeProfile(parentSketchName);
            _axis = axis;
        }

        public override string Name => _name;

        public override PartFeatureOperationEnum Operation => _operation;

        public override Profile Profile => _profile;

        public override SketchLine _AxisEntity => _axis;

        public override PartFeatureExtentEnum ExtentType => PartFeatureExtentEnum.kFullSweepExtent;
    }

    public sealed class FakeExtrudeFeatures : ExtrudeFeatures
    {
        private readonly IList<ExtrudeFeature> _items;

        public FakeExtrudeFeatures(IList<ExtrudeFeature> items)
        {
            _items = items;
        }

        public override int Count => _items.Count;

        public override ExtrudeFeature this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeExtrudeFeature : ExtrudeFeature
    {
        private readonly string _name;
        private readonly PartFeatureOperationEnum _operation;
        private readonly Profile _profile;
        private readonly ExtrudeDefinition _definition;

        public FakeExtrudeFeature(
            string name, PartFeatureOperationEnum operation, string parentSketchName, double distanceCm,
            PartFeatureExtentDirectionEnum direction = PartFeatureExtentDirectionEnum.kPositiveExtentDirection)
        {
            _name = name;
            _operation = operation;
            _profile = new FakeProfile(parentSketchName);
            _definition = new FakeExtrudeDefinition(new FakeDistanceExtent(distanceCm, direction));
        }

        public override string Name => _name;

        public override PartFeatureOperationEnum Operation => _operation;

        public override Profile Profile => _profile;

        public override ExtrudeDefinition Definition => _definition;
    }

    public sealed class FakeExtrudeDefinition : ExtrudeDefinition
    {
        private readonly PartFeatureExtent _extent;

        public FakeExtrudeDefinition(PartFeatureExtent extent)
        {
            _extent = extent;
        }

        public override PartFeatureExtentEnum ExtentType => PartFeatureExtentEnum.kDistanceExtent;

        public override PartFeatureExtent Extent => _extent;
    }

    public sealed class FakeDistanceExtent : DistanceExtent
    {
        private readonly Parameter _distance;
        private readonly PartFeatureExtentDirectionEnum _direction;

        public FakeDistanceExtent(double distanceCm, PartFeatureExtentDirectionEnum direction)
        {
            _distance = new FakeDistanceParameter(distanceCm);
            _direction = direction;
        }

        public override Parameter Distance => _distance;

        public override PartFeatureExtentDirectionEnum Direction => _direction;
    }

    public sealed class FakeDistanceParameter : Parameter
    {
        private readonly double _value;

        public FakeDistanceParameter(double value)
        {
            _value = value;
        }

        public override double _Value => _value;
    }

    public sealed class FakeProfile : Profile
    {
        private readonly PlanarSketch _parent;

        public FakeProfile(string sketchName)
        {
            _parent = new FakeNamedSketch(sketchName);
        }

        public override PlanarSketch Parent => _parent;
    }

    /// <summary>A minimal PlanarSketch fake exposing only the Name the extractor reads from a profile.</summary>
    public sealed class FakeNamedSketch : PlanarSketch
    {
        private readonly string _name;

        public FakeNamedSketch(string name)
        {
            _name = name;
        }

        public override string Name => _name;
    }

    public sealed class FakeWorkPlanes : WorkPlanes
    {
        private readonly IList<WorkPlane> _items;

        public FakeWorkPlanes(IList<WorkPlane> items)
        {
            _items = items;
        }

        public override int Count => _items.Count;

        public override WorkPlane this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeWorkPlane : WorkPlane
    {
        private readonly string _name;
        private readonly Plane _plane;

        public FakeWorkPlane(string name, double[] origin, double[] normal)
        {
            _name = name;
            _plane = new FakeWorkPlaneGeometry(origin, normal);
        }

        public override string Name => _name;

        public override Plane Plane => _plane;
    }

    /// <summary>A Plane fake giving both root point and normal (work-plane geometry).</summary>
    public sealed class FakeWorkPlaneGeometry : Plane
    {
        private readonly Point _root;
        private readonly UnitVector _normal;

        public FakeWorkPlaneGeometry(double[] origin, double[] normal)
        {
            _root = new FakePoint(origin[0], origin[1], origin[2]);
            _normal = new FakeUnitVector(normal[0], normal[1], normal[2]);
        }

        public override Point RootPoint => _root;

        public override UnitVector Normal => _normal;
    }
}
