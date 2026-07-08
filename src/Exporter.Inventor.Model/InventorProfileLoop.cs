// SPDX-License-Identifier: GPL-2.0-only
namespace Oblikovati.Exporter.Inventor.Model
{
    /// <summary>
    /// One closed loop of a feature's SELECTED profile, exactly as Inventor resolved it
    /// (Profile.ProfilePaths). <see cref="Curves"/> are the loop's curve segments in the sketch's
    /// 2-D frame (cm); <see cref="IsHole"/> is true for an inner loop (Inventor's AddsMaterial =
    /// false), false for an outer boundary.
    ///
    /// Authoring these — rather than the whole shared sketch plus a seed point — is what makes the
    /// exported profile faithful: Inventor's profile already excludes spurious/projected geometry
    /// and includes exactly the reference geometry that forms the boundary, so there is no region
    /// selection to guess and no shared-sketch ambiguity.
    /// </summary>
    public sealed class InventorProfileLoop
    {
        public bool IsHole { get; set; }

        public System.Collections.Generic.IList<InventorCurve> Curves { get; } =
            new System.Collections.Generic.List<InventorCurve>();
    }
}
