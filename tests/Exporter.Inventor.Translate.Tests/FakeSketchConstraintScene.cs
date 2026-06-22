// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Named fakes of the Inventor sketch geometric-constraint and dimension surface (subclassing
    /// the stub) so the real SketchExtractor's constraint/dimension walk runs with no Inventor.
    /// Constraints reference the exact SketchLine/SketchCircle/SketchPoint instances so the
    /// extractor's COM-identity mapping resolves them.
    /// </summary>
    public sealed class FakeGeometricConstraints : GeometricConstraints
    {
        private readonly IList<GeometricConstraint> _items;
        public FakeGeometricConstraints(IList<GeometricConstraint> items) => _items = items;
        public override int Count => _items.Count;
        public override GeometricConstraint this[int index] => _items[index - 1];
    }

    public sealed class FakeDimensionConstraints : DimensionConstraints
    {
        private readonly IList<DimensionConstraint> _items;
        public FakeDimensionConstraints(IList<DimensionConstraint> items) => _items = items;
        public override int Count => _items.Count;
        public override DimensionConstraint this[int index] => _items[index - 1];
    }

    public sealed class FakeHorizontalConstraint : HorizontalConstraint
    {
        private readonly object _entity;
        public FakeHorizontalConstraint(object entity) => _entity = entity;
        public override object Entity => _entity;
    }

    public sealed class FakeVerticalConstraint : VerticalConstraint
    {
        private readonly object _entity;
        public FakeVerticalConstraint(object entity) => _entity = entity;
        public override object Entity => _entity;
    }

    public sealed class FakeParallelConstraint : ParallelConstraint
    {
        private readonly object _a;
        private readonly object _b;
        public FakeParallelConstraint(object a, object b) { _a = a; _b = b; }
        public override object EntityOne => _a;
        public override object EntityTwo => _b;
    }

    public sealed class FakeCollinearConstraint : CollinearConstraint
    {
        private readonly object _a;
        private readonly object _b;
        public FakeCollinearConstraint(object a, object b) { _a = a; _b = b; }
        public override object EntityOne => _a;
        public override object EntityTwo => _b;
    }

    public sealed class FakeConcentricConstraint : ConcentricConstraint
    {
        private readonly object _a;
        private readonly object _b;
        public FakeConcentricConstraint(object a, object b) { _a = a; _b = b; }
        public override object EntityOne => _a;
        public override object EntityTwo => _b;
    }

    public sealed class FakeTangentConstraint : TangentConstraint
    {
        private readonly object _a;
        private readonly object _b;
        public FakeTangentConstraint(object a, object b) { _a = a; _b = b; }
        public override object EntityOne => _a;
        public override object EntityTwo => _b;
    }

    public sealed class FakeEqualLengthConstraint : EqualLengthConstraint
    {
        private readonly object _a;
        private readonly object _b;
        public FakeEqualLengthConstraint(object a, object b) { _a = a; _b = b; }
        public override object LineOne => _a;
        public override object LineTwo => _b;
    }

    public sealed class FakeEqualRadiusConstraint : EqualRadiusConstraint
    {
        private readonly object _a;
        private readonly object _b;
        public FakeEqualRadiusConstraint(object a, object b) { _a = a; _b = b; }
        public override object EntityOne => _a;
        public override object EntityTwo => _b;
    }

    public sealed class FakeSymmetryConstraint : SymmetryConstraint
    {
        private readonly object _a;
        private readonly object _b;
        private readonly object _axis;
        public FakeSymmetryConstraint(object a, object b, object axis) { _a = a; _b = b; _axis = axis; }
        public override object EntityOne => _a;
        public override object EntityTwo => _b;
        public override object SymmetryLine => _axis;
    }

    public sealed class FakeGroundConstraint : GroundConstraint
    {
        private readonly object _entity;
        public FakeGroundConstraint(object entity) => _entity = entity;
        public override object Entity => _entity;
    }

    public sealed class FakeSmoothConstraint : SmoothConstraint
    {
        private readonly object _a;
        private readonly object _b;
        public FakeSmoothConstraint(object a, object b) { _a = a; _b = b; }
        public override object EntityOne => _a;
        public override object EntityTwo => _b;
    }

    public sealed class FakeTwoLineAngleDimConstraint : TwoLineAngleDimConstraint
    {
        private readonly object _a;
        private readonly object _b;
        private readonly Parameter _parameter;
        public FakeTwoLineAngleDimConstraint(object a, object b, string expression)
        {
            _a = a;
            _b = b;
            _parameter = new FakeExpressionParameter(expression);
        }
        public override object LineOne => _a;
        public override object LineTwo => _b;
        public override Parameter Parameter => _parameter;
    }

    public sealed class FakeTwoPointDistanceDimConstraint : TwoPointDistanceDimConstraint
    {
        private readonly SketchPoint _a;
        private readonly SketchPoint _b;
        private readonly Parameter _parameter;
        public FakeTwoPointDistanceDimConstraint(SketchPoint a, SketchPoint b, string expression)
        {
            _a = a;
            _b = b;
            _parameter = new FakeExpressionParameter(expression);
        }
        public override SketchPoint PointOne => _a;
        public override SketchPoint PointTwo => _b;
        public override Parameter Parameter => _parameter;
    }

    public sealed class FakeDiameterDimConstraint : DiameterDimConstraint
    {
        private readonly object _entity;
        private readonly Parameter _parameter;
        public FakeDiameterDimConstraint(object entity, string expression)
        {
            _entity = entity;
            _parameter = new FakeExpressionParameter(expression);
        }
        public override object Entity => _entity;
        public override Parameter Parameter => _parameter;
    }

    /// <summary>A parameter that carries an expression (the dimension's driving expression).</summary>
    public sealed class FakeExpressionParameter : Parameter
    {
        private readonly string _expression;
        public FakeExpressionParameter(string expression) => _expression = expression;
        public override string Expression => _expression;
    }
}
