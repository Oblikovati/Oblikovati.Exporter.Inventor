// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Inventor;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Named fakes of the Inventor 2D-sketch surface, subclassing the all-virtual stub so the
    /// real <c>SketchExtractor</c> runs with no Inventor install. <see cref="Square"/> builds a
    /// closed four-line sketch on the XY plane for extraction tests.
    /// </summary>
    public sealed class FakePlanarSketches : PlanarSketches
    {
        private readonly IList<PlanarSketch> _items;

        public FakePlanarSketches(IList<PlanarSketch> items)
        {
            _items = items;
        }

        public override int Count => _items.Count;

        public override PlanarSketch this[object index] => _items[(int)index - 1];
    }

    public sealed class FakePlanarSketch : PlanarSketch
    {
        private readonly string _name;
        private readonly Point _origin;
        private readonly Line _axis;
        private readonly Plane _plane;
        private readonly SketchLines _lines;
        private readonly SketchCircles _circles;

        public FakePlanarSketch(
            string name, Point origin, Line axis, Plane plane, IList<SketchLine> lines, IList<SketchCircle> circles)
        {
            _name = name;
            _origin = origin;
            _axis = axis;
            _plane = plane;
            _lines = new FakeSketchLines(lines);
            _circles = new FakeSketchCircles(circles);
        }

        public override string Name => _name;

        public override Point OriginPointGeometry => _origin;

        public override Line AxisEntityGeometry => _axis;

        public override Plane PlanarEntityGeometry => _plane;

        public override SketchLines SketchLines => _lines;

        public override SketchCircles SketchCircles => _circles;

        /// <summary>A unit square (side 4 cm) of four coincident lines on the XY origin plane.</summary>
        public static FakePlanarSketch Square()
        {
            var lines = new List<SketchLine>
            {
                FakeSketchLine.From(0, 0, 4, 0),
                FakeSketchLine.From(4, 0, 4, 4),
                FakeSketchLine.From(4, 4, 0, 4),
                FakeSketchLine.From(0, 4, 0, 0),
            };
            return new FakePlanarSketch(
                "Square",
                new FakePoint(0, 0, 0),
                new FakeLine(new FakeUnitVector(1, 0, 0)),
                new FakePlane(new FakeUnitVector(0, 0, 1)),
                lines,
                new List<SketchCircle>());
        }
    }

    public sealed class FakeSketchLines : SketchLines
    {
        private readonly IList<SketchLine> _items;

        public FakeSketchLines(IList<SketchLine> items)
        {
            _items = items;
        }

        public override int Count => _items.Count;

        public override SketchLine this[int index] => _items[index - 1];
    }

    public sealed class FakeSketchLine : SketchLine
    {
        private readonly SketchPoint _start;
        private readonly SketchPoint _end;

        public FakeSketchLine(SketchPoint start, SketchPoint end)
        {
            _start = start;
            _end = end;
        }

        public override SketchPoint StartSketchPoint => _start;

        public override SketchPoint EndSketchPoint => _end;

        public override bool Construction => false;

        public static FakeSketchLine From(double x0, double y0, double x1, double y1) =>
            new FakeSketchLine(new FakeSketchPoint(x0, y0), new FakeSketchPoint(x1, y1));
    }

    public sealed class FakeSketchCircles : SketchCircles
    {
        private readonly IList<SketchCircle> _items;

        public FakeSketchCircles(IList<SketchCircle> items)
        {
            _items = items;
        }

        public override int Count => _items.Count;

        public override SketchCircle this[int index] => _items[index - 1];
    }

    public sealed class FakeSketchCircle : SketchCircle
    {
        private readonly SketchPoint _center;
        private readonly double _radius;

        public FakeSketchCircle(double cx, double cy, double radius)
        {
            _center = new FakeSketchPoint(cx, cy);
            _radius = radius;
        }

        public override SketchPoint CenterSketchPoint => _center;

        public override double Radius => _radius;

        public override bool Construction => false;
    }

    public sealed class FakeSketchPoint : SketchPoint
    {
        private readonly Point2d _geometry;

        public FakeSketchPoint(double x, double y)
        {
            _geometry = new FakePoint2d(x, y);
        }

        public override Point2d Geometry => _geometry;
    }

    public sealed class FakePoint2d : Point2d
    {
        public FakePoint2d(double x, double y)
        {
            _x = x;
            _y = y;
        }

        private readonly double _x;
        private readonly double _y;

        public override double X => _x;

        public override double Y => _y;
    }

    public sealed class FakePoint : Point
    {
        public FakePoint(double x, double y, double z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        private readonly double _x;
        private readonly double _y;
        private readonly double _z;

        public override double X => _x;

        public override double Y => _y;

        public override double Z => _z;
    }

    public sealed class FakeUnitVector : UnitVector
    {
        public FakeUnitVector(double x, double y, double z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        private readonly double _x;
        private readonly double _y;
        private readonly double _z;

        public override double X => _x;

        public override double Y => _y;

        public override double Z => _z;

        public override UnitVector CrossProduct(UnitVector o)
        {
            double cx = _y * o.Z - _z * o.Y;
            double cy = _z * o.X - _x * o.Z;
            double cz = _x * o.Y - _y * o.X;
            double len = Math.Sqrt(cx * cx + cy * cy + cz * cz);
            return len == 0 ? new FakeUnitVector(0, 0, 0) : new FakeUnitVector(cx / len, cy / len, cz / len);
        }
    }

    public sealed class FakePlane : Plane
    {
        private readonly UnitVector _normal;

        public FakePlane(UnitVector normal)
        {
            _normal = normal;
        }

        public override UnitVector Normal => _normal;
    }

    public sealed class FakeLine : Line
    {
        private readonly UnitVector _direction;

        public FakeLine(UnitVector direction)
        {
            _direction = direction;
        }

        public override UnitVector Direction => _direction;
    }
}
