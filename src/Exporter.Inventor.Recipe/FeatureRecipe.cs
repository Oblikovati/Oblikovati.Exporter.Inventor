// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Oblikovati.Exporter.Inventor.Recipe
{
    /// <summary>
    /// One history feature. Mirrors FeatureData in Oblikovati/model/feature/serialize.go: a
    /// <c>kind</c> discriminator plus exactly one typed payload. Order in the part's features
    /// list is the feature history order.
    /// </summary>
    public sealed class FeatureData
    {
        [YamlMember(Alias = "kind")]
        public string Kind { get; set; } = string.Empty;

        [YamlMember(Alias = "name", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Name { get; set; }

        [YamlMember(Alias = "extrude", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public ExtrudeData? Extrude { get; set; }

        [YamlMember(Alias = "revolve", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public RevolveData? Revolve { get; set; }

        [YamlMember(Alias = "rectangularPattern", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public RectPatternData? RectangularPattern { get; set; }

        [YamlMember(Alias = "circularPattern", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public CircPatternData? CircularPattern { get; set; }

        [YamlMember(Alias = "mirror", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public MirrorData? Mirror { get; set; }

        [YamlMember(Alias = "fillet", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public EdgeDressData? Fillet { get; set; }

        [YamlMember(Alias = "chamfer", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public EdgeDressData? Chamfer { get; set; }

        [YamlMember(Alias = "shell", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public FaceDressData? Shell { get; set; }

        [YamlMember(Alias = "draft", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public FaceDressData? Draft { get; set; }

        [YamlMember(Alias = "hole", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public HoleData? Hole { get; set; }

        [YamlMember(Alias = "sweep", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public SweepData? Sweep { get; set; }

        [YamlMember(Alias = "loft", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public LoftData? Loft { get; set; }
    }

    /// <summary>
    /// A sweep payload. Mirrors SweepData in serialize_sweep_loft.go: the profile (sketch + region
    /// index) swept along a path given as a 3D-point polyline (cm).
    /// </summary>
    public sealed class SweepData
    {
        [YamlMember(Alias = "sketch")]
        public int Sketch { get; set; }

        [YamlMember(Alias = "profile")]
        public int Profile { get; set; }

        [YamlMember(Alias = "path")]
        public IList<double[]> Path { get; } = new List<double[]>();

        [YamlMember(Alias = "closed", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Closed { get; set; }

        [YamlMember(Alias = "operation")]
        public string Operation { get; set; } = "newBody";
    }

    /// <summary>One loft section: a profile (sketch + region index). Mirrors LoftSectionData.</summary>
    public sealed class LoftSectionData
    {
        [YamlMember(Alias = "sketch")]
        public int Sketch { get; set; }

        [YamlMember(Alias = "profile")]
        public int Profile { get; set; }
    }

    /// <summary>A loft payload. Mirrors LoftData: an ordered list of profile sections.</summary>
    public sealed class LoftData
    {
        [YamlMember(Alias = "sections")]
        public IList<LoftSectionData> Sections { get; } = new List<LoftSectionData>();

        [YamlMember(Alias = "closed", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Closed { get; set; }

        [YamlMember(Alias = "operation")]
        public string Operation { get; set; } = "newBody";
    }

    /// <summary>
    /// A geometric edge descriptor (ADR-0040): the edge's midpoint and direction in centimetre
    /// database units. The Oblikovati reader binds it to a body edge at recompute.
    /// </summary>
    public sealed class GeomEdgeRefData
    {
        [YamlMember(Alias = "midpoint")]
        public double[] Midpoint { get; set; } = { 0, 0, 0 };

        [YamlMember(Alias = "direction", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double[]? Direction { get; set; }
    }

    /// <summary>A geometric face descriptor (ADR-0040): centroid + outward normal (cm).</summary>
    public sealed class GeomFaceRefData
    {
        [YamlMember(Alias = "centroid")]
        public double[] Centroid { get; set; } = { 0, 0, 0 };

        [YamlMember(Alias = "normal", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double[]? Normal { get; set; }
    }

    /// <summary>
    /// An edge dress-up payload (fillet radius / chamfer distance). Mirrors EdgeDressData in
    /// serialize_dressup.go. Edges are geometric descriptors; Value is the radius/distance (cm).
    /// </summary>
    public sealed class EdgeDressData
    {
        [YamlMember(Alias = "value")]
        public double Value { get; set; }

        [YamlMember(Alias = "geomEdges", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<GeomEdgeRefData> GeomEdges { get; } = new List<GeomEdgeRefData>();
    }

    /// <summary>
    /// A face dress-up payload (shell thickness / draft angle). Mirrors FaceDressData. Faces are
    /// geometric descriptors; Value is the thickness (cm) or angle (radians).
    /// </summary>
    public sealed class FaceDressData
    {
        [YamlMember(Alias = "value")]
        public double Value { get; set; }

        [YamlMember(Alias = "pull", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double[]? Pull { get; set; }

        [YamlMember(Alias = "geomFaces", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<GeomFaceRefData> GeomFaces { get; } = new List<GeomFaceRefData>();
    }

    /// <summary>
    /// A hole payload. Mirrors HoleData in serialize_solid.go. The placement face is a geometric
    /// descriptor (geomFace). Diameter/depth are cm.
    /// </summary>
    public sealed class HoleData
    {
        [YamlMember(Alias = "diameter")]
        public double Diameter { get; set; }

        [YamlMember(Alias = "depth")]
        public double Depth { get; set; }

        [YamlMember(Alias = "throughAll", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? ThroughAll { get; set; }

        [YamlMember(Alias = "type")]
        public string Type { get; set; } = "drilled";

        [YamlMember(Alias = "geomFace", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public GeomFaceRefData? GeomFace { get; set; }
    }

    /// <summary>
    /// A revolve payload. Mirrors RevolveData in serialize_work.go. Empty axis fields mean
    /// "revolve about the profile sketch's own centerline". Angle is in radians; 0 (unset) means
    /// a full revolution.
    /// </summary>
    public sealed class RevolveData
    {
        [YamlMember(Alias = "sketch")]
        public int Sketch { get; set; }

        [YamlMember(Alias = "profile")]
        public int Profile { get; set; }

        [YamlMember(Alias = "angle", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double? Angle { get; set; }

        [YamlMember(Alias = "operation")]
        public string Operation { get; set; } = "newBody";
    }

    /// <summary>
    /// A rectangular-pattern payload. Mirrors RectPatternData in serialize_pattern.go.
    /// <see cref="Source"/> are program indices into the features list; step vectors are the
    /// centimetre offset between adjacent copies.
    /// </summary>
    public sealed class RectPatternData
    {
        [YamlMember(Alias = "source")]
        public IList<int> Source { get; } = new List<int>();

        [YamlMember(Alias = "countX")]
        public int CountX { get; set; }

        [YamlMember(Alias = "countY")]
        public int CountY { get; set; }

        [YamlMember(Alias = "stepX")]
        public double[] StepX { get; set; } = { 0, 0, 0 };

        [YamlMember(Alias = "stepY")]
        public double[] StepY { get; set; } = { 0, 0, 0 };
    }

    /// <summary>A circular-pattern payload. Mirrors CircPatternData. Angle is radians.</summary>
    public sealed class CircPatternData
    {
        [YamlMember(Alias = "source")]
        public IList<int> Source { get; } = new List<int>();

        [YamlMember(Alias = "count")]
        public int Count { get; set; }

        [YamlMember(Alias = "angle")]
        public double Angle { get; set; }

        [YamlMember(Alias = "axisPoint")]
        public double[] AxisPoint { get; set; } = { 0, 0, 0 };

        [YamlMember(Alias = "axisDir")]
        public double[] AxisDir { get; set; } = { 0, 0, 1 };
    }

    /// <summary>
    /// A mirror payload. Mirrors MirrorData. The plane's geometry is Origin + Normal; the plane
    /// key is an identity label (Inventor has no Oblikovati lineage key to supply).
    /// </summary>
    public sealed class MirrorData
    {
        [YamlMember(Alias = "source")]
        public IList<int> Source { get; } = new List<int>();

        [YamlMember(Alias = "plane")]
        public string Plane { get; set; } = string.Empty;

        [YamlMember(Alias = "origin")]
        public double[] Origin { get; set; } = { 0, 0, 0 };

        [YamlMember(Alias = "normal")]
        public double[] Normal { get; set; } = { 1, 0, 0 };
    }

    /// <summary>
    /// An extrude payload. Mirrors ExtrudeData in serialize_extrude.go. The sketch is the array
    /// index into the part's sketches; profiles are detected region indices. Distances are
    /// centimetre database units.
    /// </summary>
    public sealed class ExtrudeData
    {
        [YamlMember(Alias = "sketch")]
        public int Sketch { get; set; }

        [YamlMember(Alias = "profiles", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<int> Profiles { get; } = new List<int>();

        [YamlMember(Alias = "operation")]
        public string Operation { get; set; } = "newBody";

        [YamlMember(Alias = "extent", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Extent { get; set; }

        [YamlMember(Alias = "direction", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Direction { get; set; }

        [YamlMember(Alias = "distance", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double? Distance { get; set; }

        [YamlMember(Alias = "distance2", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double? Distance2 { get; set; }

        [YamlMember(Alias = "taper", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double? Taper { get; set; }
    }
}
