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

        // The flat, build-order Features collection the real FeatureExtractor now walks. The scene
        // supplies features grouped by type, so present them flat in the same order the extractor
        // used to process the type collections (extrudes, revolves, patterns, mirrors, then the
        // dress-ups, then lofts and sweeps) — this keeps each test's ir.Features ordering unchanged
        // while exercising the new single-pass dispatch.
        private readonly List<object> _flat;

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
            revolves ??= new List<RevolveFeature>();
            rectPatterns ??= new List<RectangularPatternFeature>();
            circPatterns ??= new List<CircularPatternFeature>();
            mirrors ??= new List<MirrorFeature>();
            fillets ??= new List<FilletFeature>();
            chamfers ??= new List<ChamferFeature>();
            shells ??= new List<ShellFeature>();
            drafts ??= new List<FaceDraftFeature>();
            holes ??= new List<HoleFeature>();
            lofts ??= new List<LoftFeature>();
            sweeps ??= new List<SweepFeature>();

            _extrudes = new FakeExtrudeFeatures(extrudes);
            _revolves = new FakeRevolveFeatures(revolves);
            _rectPatterns = new FakeRectangularPatternFeatures(rectPatterns);
            _circPatterns = new FakeCircularPatternFeatures(circPatterns);
            _mirrors = new FakeMirrorFeatures(mirrors);
            _fillets = new FakeFilletFeatures(fillets);
            _chamfers = new FakeChamferFeatures(chamfers);
            _shells = new FakeShellFeatures(shells);
            _drafts = new FakeFaceDraftFeatures(drafts);
            _holes = new FakeHoleFeatures(holes);
            _lofts = new FakeLoftFeatures(lofts);
            _sweeps = new FakeSweepFeatures(sweeps);

            _flat = new List<object>();
            foreach (ExtrudeFeature e in extrudes) _flat.Add(e);
            foreach (RevolveFeature r in revolves) _flat.Add(r);
            foreach (RectangularPatternFeature p in rectPatterns) _flat.Add(p);
            foreach (CircularPatternFeature p in circPatterns) _flat.Add(p);
            foreach (MirrorFeature m in mirrors) _flat.Add(m);
            foreach (FilletFeature f in fillets) _flat.Add(f);
            foreach (ChamferFeature c in chamfers) _flat.Add(c);
            foreach (ShellFeature s in shells) _flat.Add(s);
            foreach (FaceDraftFeature d in drafts) _flat.Add(d);
            foreach (HoleFeature h in holes) _flat.Add(h);
            foreach (LoftFeature l in lofts) _flat.Add(l);
            foreach (SweepFeature sw in sweeps) _flat.Add(sw);
        }

        public override int Count => _flat.Count;

        public override object this[int index] => _flat[index - 1];

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
            : this(name, profileSketchName, new FakePath(pathPoints)) { }
        public FakeSweepFeature(string name, string profileSketchName, Path path)
        {
            _name = name;
            _profile = new FakeProfile(profileSketchName);
            _path = path;
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
        private readonly object _entity;
        private readonly bool _opposed;
        public FakePathEntity(double[] a, double[] b) : this(new FakePathLine(a, b), false) { }
        public FakePathEntity(object entity, bool opposed)
        {
            _entity = entity;
            _opposed = opposed;
        }
        public override object SketchEntity => _entity;
        public override bool OpposedToSketchEntity => _opposed;
    }

    /// <summary>A sweep path made of explicit entities (e.g. arc/spline segments) in order.</summary>
    public sealed class FakeEntityPath : Path
    {
        private readonly IList<PathEntity> _entities;
        public FakeEntityPath(IList<PathEntity> entities) => _entities = entities;
        public override int Count => _entities.Count;
        public override PathEntity this[int index] => _entities[index - 1];
    }

    /// <summary>A sketch arc whose only role is its tessellated 3D geometry (a sweep-path segment).</summary>
    public sealed class FakePathArc : SketchArc
    {
        private readonly Arc3d _geometry;
        public FakePathArc(double[][] strokePoints) => _geometry = new FakeArc3d(strokePoints);
        public override Arc3d Geometry3d => _geometry;
    }

    /// <summary>A sketch spline whose only role is its tessellated 3D geometry (a sweep-path segment).</summary>
    public sealed class FakePathSpline : SketchSpline
    {
        private readonly BSplineCurve _geometry;
        public FakePathSpline(double[][] strokePoints) => _geometry = new FakeBSplineCurve(strokePoints);
        public override BSplineCurve Geometry3d => _geometry;
    }

    public sealed class FakeArc3d : Arc3d
    {
        private readonly CurveEvaluator _evaluator;
        public FakeArc3d(double[][] strokePoints) => _evaluator = new FakeCurveEvaluator(strokePoints);
        public override CurveEvaluator Evaluator => _evaluator;
    }

    public sealed class FakeBSplineCurve : BSplineCurve
    {
        private readonly CurveEvaluator _evaluator;
        public FakeBSplineCurve(double[][] strokePoints) => _evaluator = new FakeCurveEvaluator(strokePoints);
        public override CurveEvaluator Evaluator => _evaluator;
    }

    /// <summary>An evaluator that returns a fixed polyline as its strokes (x,y,z flattened).</summary>
    public sealed class FakeCurveEvaluator : CurveEvaluator
    {
        private readonly double[][] _points;
        public FakeCurveEvaluator(double[][] points) => _points = points;
        public override void GetParamExtents(out double minParam, out double maxParam)
        {
            minParam = 0;
            maxParam = 1;
        }
        public override void GetStrokes(
            double fromParam, double toParam, double tolerance, out int vertexCount, out double[] vertexCoordinates)
        {
            vertexCount = _points.Length;
            vertexCoordinates = new double[_points.Length * 3];
            for (int i = 0; i < _points.Length; i++)
            {
                vertexCoordinates[i * 3] = _points[i][0];
                vertexCoordinates[(i * 3) + 1] = _points[i][1];
                vertexCoordinates[(i * 3) + 2] = _points[i][2];
            }
        }
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
            : this(name, operation, parentSketchName, new FakeDistanceExtent(distanceCm, direction))
        {
        }

        public FakeExtrudeFeature(
            string name, PartFeatureOperationEnum operation, string parentSketchName, PartFeatureExtent extent)
            : this(name, operation, parentSketchName, extent, null)
        {
        }

        public FakeExtrudeFeature(
            string name, PartFeatureOperationEnum operation, string parentSketchName, PartFeatureExtent extent,
            IList<ProfilePath>? paths)
        {
            _name = name;
            _operation = operation;
            _profile = new FakeProfile(parentSketchName, paths);
            _definition = new FakeExtrudeDefinition(extent);
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

    public sealed class FakeThroughAllExtent : ThroughAllExtent
    {
        private readonly PartFeatureExtentDirectionEnum _direction;
        public FakeThroughAllExtent(PartFeatureExtentDirectionEnum direction) => _direction = direction;
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
        private readonly IList<ProfilePath> _paths;

        public FakeProfile(string sketchName, IList<ProfilePath>? paths = null)
        {
            _parent = new FakeNamedSketch(sketchName);
            _paths = paths ?? new List<ProfilePath>();
        }

        public override PlanarSketch Parent => _parent;

        public override int Count => _paths.Count;

        public override System.Collections.IEnumerator GetEnumerator() => _paths.GetEnumerator();
    }

    public sealed class FakeProfilePath : ProfilePath
    {
        private readonly IList<ProfileEntity> _entities;

        public FakeProfilePath(bool addsMaterial, IList<ProfileEntity> entities)
        {
            AddsMaterialValue = addsMaterial;
            _entities = entities;
        }

        public bool AddsMaterialValue { get; }

        public override bool AddsMaterial => AddsMaterialValue;

        public override int Count => _entities.Count;

        public override System.Collections.IEnumerator GetEnumerator() => _entities.GetEnumerator();
    }

    public sealed class FakeProfileEntity : ProfileEntity
    {
        private readonly SketchPoint _start;

        public FakeProfileEntity(double x, double y) => _start = new FakeSketchPoint(x, y);

        public override SketchPoint StartSketchPoint => _start;
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
