// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// The (area, hole count) of the sketch region an interior seed point falls in, computed
    /// from the IR curves the emitter authored — so the emitter can match that region against
    /// the live-solved profiles (which report area + holes) by area, rather than trusting a
    /// fragile precomputed index. This slice handles regions bounded by straight-line loops and
    /// circles (the profiles this feature slice emits); arcs/splines/ellipses in a profile are a
    /// later refinement (they need chording to a polygon before this containment applies).
    /// </summary>
    public readonly struct RegionKey
    {
        public RegionKey(double area, int holes)
        {
            Area = area;
            Holes = holes;
        }

        /// <summary>Net area of the region (outer loop minus its inner hole loops), cm².</summary>
        public double Area { get; }

        /// <summary>Number of inner hole loops bounding the region.</summary>
        public int Holes { get; }
    }

    /// <summary>Reduces IR sketch curves to loops so a seed point resolves to its region.</summary>
    public static class SketchGeometry
    {
        /// <summary>
        /// Returns the region containing <paramref name="seed"/> (a 2D interior point in sketch
        /// cm): the smallest loop that contains it, less any loops nested directly inside that do
        /// not contain the seed (its holes). Throws if the seed is in no loop or the sketch has an
        /// unsupported profile curve.
        /// </summary>
        public static RegionKey RegionForSeed(InventorSketch sketch, double[] seed)
        {
            if (sketch == null)
                throw new ArgumentNullException(nameof(sketch));
            if (seed == null || seed.Length != 2)
                throw new ArgumentException("seed must be a 2D [x,y] point.", nameof(seed));

            IReadOnlyList<Loop> loops = BuildLoops(sketch);
            Loop container = SmallestContaining(loops, seed);
            double holesArea = 0;
            int holes = 0;
            foreach (Loop loop in loops)
            {
                if (!ReferenceEquals(loop, container) && container.Contains(loop.Inside) && !loop.Contains(seed))
                {
                    holesArea += loop.Area;
                    holes++;
                }
            }
            return new RegionKey(container.Area - holesArea, holes);
        }

        private static Loop SmallestContaining(IReadOnlyList<Loop> loops, double[] seed)
        {
            Loop? best = null;
            foreach (Loop loop in loops)
            {
                if (loop.Contains(seed) && (best == null || loop.Area < best.Area))
                    best = loop;
            }
            if (best == null)
                throw new InvalidOperationException($"seed ({seed[0]}, {seed[1]}) is inside no sketch loop.");
            return best;
        }

        // Assembles profile loops from the sketch's real (non-construction, non-centerline)
        // curves: each circle is its own loop; the line segments are chained by shared endpoints
        // into polygon loops.
        private static IReadOnlyList<Loop> BuildLoops(InventorSketch sketch)
        {
            var loops = new List<Loop>();
            var segments = new List<InventorCurve>();
            foreach (InventorCurve c in sketch.Curves)
            {
                if (c.Construction || c.Centerline)
                    continue;
                if (c.Kind == InventorCurveKind.Circle)
                    loops.Add(Loop.Circle(c.Center, c.Radius));
                else if (c.Kind == InventorCurveKind.Line)
                    segments.Add(c);
                else
                    throw new InvalidOperationException($"profile curve kind {c.Kind} is not supported in this slice.");
            }
            loops.AddRange(LoopAssembler.FromSegments(segments));
            return loops;
        }
    }
}
