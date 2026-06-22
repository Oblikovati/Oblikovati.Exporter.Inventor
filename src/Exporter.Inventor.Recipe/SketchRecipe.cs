// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Oblikovati.Exporter.Inventor.Recipe
{
    /// <summary>
    /// One 2D sketch. Mirrors SketchData in Oblikovati/model/sketch/serialize.go. Each curve
    /// keeps its own defining points; coincidence is expressed by <c>coincident</c> constraints
    /// between distinct points (not by sharing ids). Coordinates are in centimetres (database
    /// units).
    /// </summary>
    public sealed class SketchData
    {
        [YamlMember(Alias = "id")]
        public int Id { get; set; }

        [YamlMember(Alias = "name", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Name { get; set; }

        [YamlMember(Alias = "plane")]
        public PlaneData Plane { get; set; } = new PlaneData();

        [YamlMember(Alias = "points", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<PointData> Points { get; } = new List<PointData>();

        [YamlMember(Alias = "entities", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<EntityData> Entities { get; } = new List<EntityData>();

        [YamlMember(Alias = "constraints", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<ConstraintData> Constraints { get; } = new List<ConstraintData>();

        [YamlMember(Alias = "dimensions", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<DimensionData> Dimensions { get; } = new List<DimensionData>();
    }

    /// <summary>Sketch plane as origin + two in-plane axes (model space).</summary>
    public sealed class PlaneData
    {
        [YamlMember(Alias = "origin")]
        public double[] Origin { get; set; } = { 0, 0, 0 };

        [YamlMember(Alias = "xAxis")]
        public double[] XAxis { get; set; } = { 1, 0, 0 };

        [YamlMember(Alias = "yAxis")]
        public double[] YAxis { get; set; } = { 0, 1, 0 };
    }

    /// <summary>One constrainable point.</summary>
    public sealed class PointData
    {
        [YamlMember(Alias = "id")]
        public int Id { get; set; }

        [YamlMember(Alias = "x")]
        public double X { get; set; }

        [YamlMember(Alias = "y")]
        public double Y { get; set; }

        [YamlMember(Alias = "standalone", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Standalone { get; set; }
    }

    /// <summary>
    /// One curve entity. <see cref="Points"/> lists defining point ids in a kind-specific order
    /// (line: A,B; circle: center; arc: center,start,end). Optional fields are nullable so they
    /// are emitted only for the kinds that use them.
    /// </summary>
    public sealed class EntityData
    {
        [YamlMember(Alias = "id")]
        public int Id { get; set; }

        [YamlMember(Alias = "kind")]
        public string Kind { get; set; } = string.Empty;

        [YamlMember(Alias = "points", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<int> Points { get; } = new List<int>();

        [YamlMember(Alias = "radius", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double? Radius { get; set; }

        [YamlMember(Alias = "ccw", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Ccw { get; set; }

        [YamlMember(Alias = "construction", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Construction { get; set; }

        [YamlMember(Alias = "centerline", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Centerline { get; set; }
    }

    /// <summary>
    /// One geometric constraint. Operand ids split into Points (point operands) and Curves
    /// (entity operands), in the order the restorer expects per kind.
    /// </summary>
    public sealed class ConstraintData
    {
        [YamlMember(Alias = "kind")]
        public string Kind { get; set; } = string.Empty;

        [YamlMember(Alias = "points", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<int> Points { get; } = new List<int>();

        [YamlMember(Alias = "curves", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<int> Curves { get; } = new List<int>();
    }

    /// <summary>One dimensional constraint linking geometry to a parameter expression.</summary>
    public sealed class DimensionData
    {
        [YamlMember(Alias = "kind")]
        public string Kind { get; set; } = string.Empty;

        [YamlMember(Alias = "points", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<int> Points { get; } = new List<int>();

        [YamlMember(Alias = "curves", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<int> Curves { get; } = new List<int>();

        // The Go field has no omitempty, so the key is always written (even if empty).
        [YamlMember(Alias = "expression")]
        public string Expression { get; set; } = string.Empty;

        [YamlMember(Alias = "driven", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Driven { get; set; }
    }
}
