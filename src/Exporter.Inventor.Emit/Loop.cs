// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// A closed loop in sketch 2D (cm) — either a circle or a straight-sided polygon — with the
    /// two queries region resolution needs: its enclosed <see cref="Area"/> and whether it
    /// <see cref="Contains"/> a point. <see cref="Inside"/> is one representative interior point,
    /// used to test whether one loop nests inside another.
    /// </summary>
    internal sealed class Loop
    {
        private readonly double[][]? _vertices; // polygon
        private readonly double[] _center;      // circle
        private readonly double _radius;        // circle
        private readonly bool _isCircle;

        private Loop(double[][]? vertices, double[] center, double radius, bool isCircle, double area, double[] inside)
        {
            _vertices = vertices;
            _center = center;
            _radius = radius;
            _isCircle = isCircle;
            Area = area;
            Inside = inside;
        }

        public double Area { get; }

        /// <summary>A point strictly inside the loop (a circle's centre, a polygon's centroid).</summary>
        public double[] Inside { get; }

        public static Loop Circle(double[] center, double radius) =>
            new Loop(null, new[] { center[0], center[1] }, radius, true, Math.PI * radius * radius,
                new[] { center[0], center[1] });

        public static Loop Polygon(double[][] vertices)
        {
            double area = Math.Abs(SignedArea(vertices));
            return new Loop(vertices, EmptyPoint, 0, false, area, Centroid(vertices));
        }

        public bool Contains(double[] p) => _isCircle ? InCircle(p) : InPolygon(p);

        private bool InCircle(double[] p)
        {
            double dx = p[0] - _center[0];
            double dy = p[1] - _center[1];
            return dx * dx + dy * dy <= _radius * _radius;
        }

        // Even-odd ray cast: counts polygon edges a rightward ray from p crosses.
        private bool InPolygon(double[] p)
        {
            double[][] v = _vertices!;
            bool inside = false;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            {
                bool straddles = (v[i][1] > p[1]) != (v[j][1] > p[1]);
                if (!straddles)
                    continue;
                double x = (v[j][0] - v[i][0]) * (p[1] - v[i][1]) / (v[j][1] - v[i][1]) + v[i][0];
                if (p[0] < x)
                    inside = !inside;
            }
            return inside;
        }

        private static double SignedArea(double[][] v)
        {
            double sum = 0;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
                sum += (v[j][0] * v[i][1]) - (v[i][0] * v[j][1]);
            return sum / 2.0;
        }

        private static double[] Centroid(double[][] v)
        {
            double x = 0, y = 0;
            foreach (double[] p in v)
            {
                x += p[0];
                y += p[1];
            }
            return new[] { x / v.Length, y / v.Length };
        }

        private static readonly double[] EmptyPoint = { 0, 0 };
    }

    /// <summary>Chains line segments sharing endpoints into closed polygon loops.</summary>
    internal static class LoopAssembler
    {
        private const double Tolerance = 1e-6;

        /// <summary>
        /// Walks the segments end-to-end into closed rings. A ring closes when the walk returns to
        /// its start point; segments that never close are ignored (they bound no region).
        /// </summary>
        public static IEnumerable<Loop> FromSegments(IReadOnlyList<Model.InventorCurve> segments)
        {
            var used = new bool[segments.Count];
            var loops = new List<Loop>();
            for (int i = 0; i < segments.Count; i++)
            {
                if (used[i])
                    continue;
                if (TryWalk(segments, used, i, out double[][] ring))
                    loops.Add(Loop.Polygon(ring));
            }
            return loops;
        }

        private static bool TryWalk(IReadOnlyList<Model.InventorCurve> segs, bool[] used, int start, out double[][] ring)
        {
            var pts = new List<double[]>();
            used[start] = true;
            double[] first = segs[start].Start;
            double[] cursor = segs[start].End;
            pts.Add(first);
            while (!Same(cursor, first))
            {
                pts.Add(cursor);
                if (!TryExtend(segs, used, ref cursor))
                {
                    ring = System.Array.Empty<double[]>();
                    return false;
                }
            }
            ring = pts.ToArray();
            return ring.Length >= 3;
        }

        // Finds the unused segment touching cursor and advances cursor to its far endpoint.
        private static bool TryExtend(IReadOnlyList<Model.InventorCurve> segs, bool[] used, ref double[] cursor)
        {
            for (int k = 0; k < segs.Count; k++)
            {
                if (used[k])
                    continue;
                if (Same(segs[k].Start, cursor)) { used[k] = true; cursor = segs[k].End; return true; }
                if (Same(segs[k].End, cursor)) { used[k] = true; cursor = segs[k].Start; return true; }
            }
            return false;
        }

        private static bool Same(double[] a, double[] b) =>
            Math.Abs(a[0] - b[0]) < Tolerance && Math.Abs(a[1] - b[1]) < Tolerance;
    }
}
