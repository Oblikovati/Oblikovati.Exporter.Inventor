// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;

namespace Oblikovati.Exporter.Inventor.Translate
{
    using Slot = System.ValueTuple<long, InventorCurvePointRole>;

    /// <summary>
    /// Allocates one distinct recipe point per curve endpoint/center. This mirrors how the
    /// Oblikovati engine itself serializes sketches: each curve keeps its own points and
    /// coincidence is expressed by `coincident` CONSTRAINTS, not by sharing ids (confirmed by
    /// round-tripping an engine-authored rectangle — merging endpoints into shared ids instead
    /// yields zero detected profiles). Coordinates pass straight through: Inventor's database
    /// unit (centimetres) is already the recipe's unit.
    /// </summary>
    public sealed class SketchPointTable
    {
        private readonly Dictionary<Slot, int> _slotToPointId = new Dictionary<Slot, int>();
        private readonly List<PointData> _points = new List<PointData>();

        public IReadOnlyList<PointData> Points => _points;

        public int PointId(InventorPointRef reference) => _slotToPointId[(reference.CurveId, reference.Role)];

        public void Build(InventorSketch sketch, IdAllocator ids)
        {
            foreach (InventorCurve curve in sketch.Curves)
            {
                foreach (InventorCurvePointRole role in RolesOf(curve.Kind))
                {
                    int id = ids.Next();
                    _slotToPointId[(curve.Id, role)] = id;
                    double[] xy = CoordOf(curve, role);
                    _points.Add(new PointData { Id = id, X = xy[0], Y = xy[1] });
                }
            }
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
