// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.Inventor.Model
{
    /// <summary>
    /// Kind of an extracted Inventor document. Mirrors Oblikovati's document types so the
    /// translator can pick the right recipe envelope (.opd part vs .oad assembly).
    /// </summary>
    public enum InventorDocumentKind
    {
        Part = 1,
        Assembly = 2,
    }

    /// <summary>
    /// The Inventor-neutral root of one extracted document. The Inventor adapter populates
    /// this from a live session; the translator consumes only this (never the Inventor API),
    /// so the translation core builds and is unit-tested on any runner.
    ///
    /// Example:
    /// <code>
    /// var doc = new InventorDocument { DisplayName = "bracket", Kind = InventorDocumentKind.Part };
    /// </code>
    /// </summary>
    public sealed class InventorDocument
    {
        public string DisplayName { get; set; } = string.Empty;

        public InventorDocumentKind Kind { get; set; } = InventorDocumentKind.Part;

        /// <summary>Length unit abbreviation as reported by Inventor (e.g. "mm", "in").</summary>
        public string LengthUnit { get; set; } = "mm";

        /// <summary>Angle unit abbreviation as reported by Inventor (e.g. "deg", "rad").</summary>
        public string AngleUnit { get; set; } = "deg";

        /// <summary>Model parameters extracted from the part, in Inventor order.</summary>
        public IList<InventorParameter> Parameters { get; } = new List<InventorParameter>();

        /// <summary>2D sketches extracted from the part, in creation order.</summary>
        public IList<InventorSketch> Sketches { get; } = new List<InventorSketch>();

        /// <summary>User work planes (datums) extracted from the part, in creation order.</summary>
        public IList<InventorWorkPlane> WorkPlanes { get; } = new List<InventorWorkPlane>();

        /// <summary>Feature history (extrudes, …) in creation order.</summary>
        public IList<InventorFeature> Features { get; } = new List<InventorFeature>();
    }

    /// <summary>
    /// One Inventor model parameter (the Inventor equivalent of an Oblikovati parameter).
    /// The <see cref="Expression"/> is the raw Inventor expression and may reference other
    /// parameters by name (e.g. "width * 2").
    /// </summary>
    public sealed class InventorParameter
    {
        public string Name { get; set; } = string.Empty;

        public string Expression { get; set; } = string.Empty;

        /// <summary>Unit abbreviation Inventor associates with the parameter value.</summary>
        public string Unit { get; set; } = string.Empty;
    }
}
