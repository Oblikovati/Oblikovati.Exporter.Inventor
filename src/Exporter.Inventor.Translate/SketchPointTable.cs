// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    using Slot = System.ValueTuple<long, InventorCurvePointRole>;

    /// <summary>
    /// Allocates one distinct recipe point per curve endpoint/center (and per spline fit point).
    /// This mirrors how the Oblikovati engine itself serializes sketches: each curve keeps its own
    /// points and coincidence is expressed by `coincident` CONSTRAINTS, not by sharing ids.
    /// Coordinates pass straight through: Inventor's database unit (centimetres) is the recipe's.
    /// </summary>
    public sealed class SketchPointTable
    {
        private readonly Dictionary<Slot, int> _slotToPointId = new Dictionary<Slot, int>();
        private readonly Dictionary<long, List<int>> _splinePointIds = new Dictionary<long, List<int>>();
        private readonly List<PointData> _points = new List<PointData>();

        public IReadOnlyList<PointData> Points => _points;

        public int PointId(InventorPointRef reference)
        {
            if (reference.Role == InventorCurvePointRole.SplinePoint)
            {
                return _splinePointIds[reference.CurveId][reference.Index];
            }

            return _slotToPointId[(reference.CurveId, reference.Role)];
        }

        /// <summary>The ordered recipe point ids of a spline's fit points.</summary>
        public IReadOnlyList<int> SplinePointIds(long curveId) => _splinePointIds[curveId];

        public void Build(InventorSketch sketch, IdAllocator ids)
        {
            foreach (InventorCurve curve in sketch.Curves)
            {
                if (curve.Kind == InventorCurveKind.Spline)
                {
                    BuildSpline(curve, ids);
                    continue;
                }

                foreach (InventorCurvePointRole role in RolesOf(curve.Kind))
                {
                    int id = ids.Next();
                    _slotToPointId[(curve.Id, role)] = id;
                    double[] xy = CoordOf(curve, role);
                    _points.Add(new PointData { Id = id, X = xy[0], Y = xy[1] });
                }
            }
        }

        private void BuildSpline(InventorCurve curve, IdAllocator ids)
        {
            var ordered = new List<int>(curve.SplinePoints.Count);
            foreach (double[] p in curve.SplinePoints)
            {
                int id = ids.Next();
                ordered.Add(id);
                _points.Add(new PointData { Id = id, X = p[0], Y = p[1] });
            }

            _splinePointIds[curve.Id] = ordered;
        }

        private static double[] CoordOf(InventorCurve curve, InventorCurvePointRole role)
        {
            switch (role)
            {
                case InventorCurvePointRole.Start: return curve.Start;
                case InventorCurvePointRole.End: return curve.End;
                default: return curve.Center;
            }
        }

        private static IEnumerable<InventorCurvePointRole> RolesOf(InventorCurveKind kind)
        {
            switch (kind)
            {
                case InventorCurveKind.Line:
                    yield return InventorCurvePointRole.Start;
                    yield return InventorCurvePointRole.End;
                    break;
                case InventorCurveKind.Circle:
                    yield return InventorCurvePointRole.Center;
                    break;
                default: // Arc
                    yield return InventorCurvePointRole.Center;
                    yield return InventorCurvePointRole.Start;
                    yield return InventorCurvePointRole.End;
                    break;
            }
        }
    }
}
