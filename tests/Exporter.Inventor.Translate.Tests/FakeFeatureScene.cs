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

        public FakePartFeatures(
            IList<ExtrudeFeature> extrudes,
            IList<RevolveFeature>? revolves = null,
            IList<RectangularPatternFeature>? rectPatterns = null,
            IList<CircularPatternFeature>? circPatterns = null,
            IList<MirrorFeature>? mirrors = null,
            IList<FilletFeature>? fillets = null,
            IList<ChamferFeature>? chamfers = null,
            IList<ShellFeature>? shells = null)
        {
            _extrudes = new FakeExtrudeFeatures(extrudes);
            _revolves = new FakeRevolveFeatures(revolves ?? new List<RevolveFeature>());
            _rectPatterns = new FakeRectangularPatternFeatures(rectPatterns ?? new List<RectangularPatternFeature>());
            _circPatterns = new FakeCircularPatternFeatures(circPatterns ?? new List<CircularPatternFeature>());
            _mirrors = new FakeMirrorFeatures(mirrors ?? new List<MirrorFeature>());
            _fillets = new FakeFilletFeatures(fillets ?? new List<FilletFeature>());
            _chamfers = new FakeChamferFeatures(chamfers ?? new List<ChamferFeature>());
            _shells = new FakeShellFeatures(shells ?? new List<ShellFeature>());
        }

        public override ExtrudeFeatures ExtrudeFeatures => _extrudes;

        public override RevolveFeatures RevolveFeatures => _revolves;

        public override RectangularPatternFeatures RectangularPatternFeatures => _rectPatterns;

        public override CircularPatternFeatures CircularPatternFeatures => _circPatterns;

        public override MirrorFeatures MirrorFeatures => _mirrors;

        public override FilletFeatures FilletFeatures => _fillets;

        public override ChamferFeatures ChamferFeatures => _chamfers;

        public override ShellFeatures ShellFeatures => _shells;
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
