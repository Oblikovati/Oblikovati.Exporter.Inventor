// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Oblikovati.Exporter.Inventor.Emit
{
    /// <summary>
    /// One solved sketch profile as <c>list_sketch_profiles</c> reports it: an index the extrude
    /// consumes, the region's net area (cm²), whether it is closed, and its inner-loop (hole)
    /// count. These are the ALREADY-SOLVED regions — selecting against them is the point of the
    /// live pivot (the host's region ordering is not predictable from outside).
    /// </summary>
    public readonly struct LiveProfile
    {
        public LiveProfile(int index, double area, bool closed, int holes)
        {
            Index = index;
            Area = area;
            Closed = closed;
            Holes = holes;
        }

        public int Index { get; }

        public double Area { get; }

        public bool Closed { get; }

        public int Holes { get; }
    }

    /// <summary>Parses the <c>list_sketch_profiles</c> JSON result into <see cref="LiveProfile"/> rows.</summary>
    public static class ProfileList
    {
        /// <summary>
        /// Reads the <c>{"profiles":[{index,area,closed,holes},…]}</c> structured result. Throws if
        /// the shape is not the expected profiles array.
        /// </summary>
        public static IReadOnlyList<LiveProfile> Parse(JsonElement result)
        {
            if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty("profiles", out JsonElement arr) ||
                arr.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("list_sketch_profiles result has no 'profiles' array: " + result);
            }
            var profiles = new List<LiveProfile>(arr.GetArrayLength());
            foreach (JsonElement p in arr.EnumerateArray())
                profiles.Add(ReadProfile(p));
            return profiles;
        }

        private static LiveProfile ReadProfile(JsonElement p) => new LiveProfile(
            index: Int(p, "index"),
            area: Double(p, "area"),
            closed: p.TryGetProperty("closed", out JsonElement c) && c.ValueKind == JsonValueKind.True,
            holes: Int(p, "holes"));

        private static int Int(JsonElement p, string name) =>
            p.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

        private static double Double(JsonElement p, string name) =>
            p.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0.0;
    }
}
