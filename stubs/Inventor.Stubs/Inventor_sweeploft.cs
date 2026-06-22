// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor loft read surface. A loft's Sections is a heterogeneous
// ObjectCollection whose profile items are cast to Profile and resolved to their parent sketch.
namespace Inventor
{
    /// <summary>Stub of the loft-features collection (object-indexed, 1-based).</summary>
    public class LoftFeatures
    {
        public virtual int Count => throw Stub.Error();

        public virtual LoftFeature this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one loft feature: its ordered profile sections and the boolean operation.</summary>
    public class LoftFeature
    {
        public virtual string Name => throw Stub.Error();

        public virtual PartFeatureOperationEnum Operation => throw Stub.Error();

        public virtual ObjectCollection Sections => throw Stub.Error();
    }
}
