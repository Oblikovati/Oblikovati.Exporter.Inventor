// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Named fakes of the Inventor pattern/mirror surface (subclassing the stub) so the real
    /// FeatureExtractor's pattern walk runs with no Inventor install.
    /// </summary>
    public sealed class FakeRectangularPatternFeatures : RectangularPatternFeatures
    {
        private readonly IList<RectangularPatternFeature> _items;
        public FakeRectangularPatternFeatures(IList<RectangularPatternFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override RectangularPatternFeature this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeCircularPatternFeatures : CircularPatternFeatures
    {
        private readonly IList<CircularPatternFeature> _items;
        public FakeCircularPatternFeatures(IList<CircularPatternFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override CircularPatternFeature this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeMirrorFeatures : MirrorFeatures
    {
        private readonly IList<MirrorFeature> _items;
        public FakeMirrorFeatures(IList<MirrorFeature> items) => _items = items;
        public override int Count => _items.Count;
        public override MirrorFeature this[object index] => _items[(int)index - 1];
    }

    public sealed class FakeRectangularPatternFeature : RectangularPatternFeature
    {
        private readonly string _name;
        private readonly ObjectCollection _parents;
        private readonly object _xDir;

        public FakeRectangularPatternFeature(string name, string[] parentNames, int countX, double xSpacing, object xDir)
        {
            _name = name;
            _parents = new FakeObjectCollection(parentNames);
            _xDir = xDir;
            XCountValue = countX;
            XSpacingValue = xSpacing;
        }

        public int XCountValue { get; }
        public double XSpacingValue { get; }

        public override string Name => _name;
        public override ObjectCollection ParentFeatures => _parents;
        public override Parameter XCount => new FakeDistanceParameter(XCountValue);
        public override Parameter YCount => new FakeDistanceParameter(1);
        public override Parameter XSpacing => new FakeDistanceParameter(XSpacingValue);
        public override Parameter YSpacing => new FakeDistanceParameter(0);
        public override object XDirectionEntity => _xDir;
        public override object YDirectionEntity => _xDir;
        public override bool NaturalXDirection => true;
        public override bool NaturalYDirection => true;
    }

    public sealed class FakeCircularPatternFeature : CircularPatternFeature
    {
        private readonly string _name;
        private readonly ObjectCollection _parents;
        private readonly object _axis;
        private readonly double _count;
        private readonly double _angle;

        public FakeCircularPatternFeature(string name, string[] parentNames, int count, double angle, object axis)
        {
            _name = name;
            _parents = new FakeObjectCollection(parentNames);
            _axis = axis;
            _count = count;
            _angle = angle;
        }

        public override string Name => _name;
        public override ObjectCollection ParentFeatures => _parents;
        public override Parameter Count => new FakeDistanceParameter(_count);
        public override Parameter Angle => new FakeDistanceParameter(_angle);
        public override object AxisEntity => _axis;
        public override bool NaturalAxisDirection => true;
    }

    public sealed class FakeMirrorFeature : MirrorFeature
    {
        private readonly string _name;
        private readonly ObjectCollection _parents;
        private readonly object _plane;

        public FakeMirrorFeature(string name, string[] parentNames, object plane)
        {
            _name = name;
            _parents = new FakeObjectCollection(parentNames);
            _plane = plane;
        }

        public override string Name => _name;
        public override ObjectCollection ParentFeatures => _parents;
        public override object MirrorPlaneEntity => _plane;
    }

    /// <summary>An object collection of PartFeature references built from feature names.</summary>
    public sealed class FakeObjectCollection : ObjectCollection
    {
        private readonly object[] _items;
        public FakeObjectCollection(string[] featureNames)
        {
            _items = new object[featureNames.Length];
            for (int i = 0; i < featureNames.Length; i++) _items[i] = new FakePartFeatureRef(featureNames[i]);
        }
        public override int Count => _items.Length;
        public override object this[int index] => _items[index - 1];
    }

    public sealed class FakePartFeatureRef : PartFeature
    {
        private readonly string _name;
        public FakePartFeatureRef(string name) => _name = name;
        public override string Name => _name;
    }

    /// <summary>A work axis through a point with a direction (its Line gives both).</summary>
    public sealed class FakeWorkAxis : WorkAxis
    {
        private readonly Line _line;
        public FakeWorkAxis(double[] point, double[] direction) => _line = new FakeAxisLine(point, direction);
        public override Line Line => _line;
    }

    public sealed class FakeAxisLine : Line
    {
        private readonly Point _root;
        private readonly UnitVector _dir;
        public FakeAxisLine(double[] point, double[] direction)
        {
            _root = new FakePoint(point[0], point[1], point[2]);
            _dir = new FakeUnitVector(direction[0], direction[1], direction[2]);
        }
        public override Point RootPoint => _root;
        public override UnitVector Direction => _dir;
    }
}
