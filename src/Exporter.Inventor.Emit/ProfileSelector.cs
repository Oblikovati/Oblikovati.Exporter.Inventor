// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// Selects which live-solved profile an extrude should consume. When the IR carries an interior
    /// seed, the emitter resolves the seed to a <see cref="RegionKey"/> (area + holes) against the
    /// authored geometry and matches it here to the live profile by hole count then nearest area —
    /// robust to the host's unpredictable region ordering. With no seed it falls back to the IR's
    /// precomputed index.
    /// </summary>
    public static class ProfileSelector
    {
        /// <summary>
        /// Picks the profile that best matches <paramref name="target"/>: prefer an equal hole
        /// count, then the smallest absolute area difference. Throws if there are no profiles.
        /// </summary>
        public static int SelectByRegion(IReadOnlyList<LiveProfile> profiles, RegionKey target)
        {
            Require(profiles);
            int best = -1;
            double bestScore = double.PositiveInfinity;
            foreach (LiveProfile p in profiles)
            {
                double score = Score(p, target);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = p.Index;
                }
            }
            return best;
        }

        /// <summary>Returns <paramref name="index"/> when a profile carries it; else throws.</summary>
        public static int SelectByIndex(IReadOnlyList<LiveProfile> profiles, int index)
        {
            Require(profiles);
            foreach (LiveProfile p in profiles)
            {
                if (p.Index == index)
                    return index;
            }
            throw new InvalidOperationException(
                $"profile index {index} is not among the {profiles.Count} solved profile(s).");
        }

        // A hole-count mismatch is a whole "area unit" penalty so an equal-hole profile always wins
        // over a nearer-area one with the wrong hole count (a disk vs the annulus that shares a rim).
        private static double Score(LiveProfile p, RegionKey target)
        {
            double areaDiff = Math.Abs(p.Area - target.Area);
            double holePenalty = p.Holes == target.Holes ? 0.0 : 1.0 + Math.Abs(p.Area) + Math.Abs(target.Area);
            return areaDiff + holePenalty;
        }

        private static void Require(IReadOnlyList<LiveProfile> profiles)
        {
            if (profiles == null || profiles.Count == 0)
                throw new InvalidOperationException("the sketch produced no solved profiles to select from.");
        }
    }
}
