// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor parameter + units read surface the adapter walks.
// Shapes mirror the genuine Autodesk.Inventor.Interop (verified by compiling the adapter
// against the real assembly): UserParameters is 1-based and indexed by object, UserParameter
// exposes Name/Expression directly, and Units imports as a get_Units() method because its COM
// accessors are asymmetric (get → string, set → object).
namespace Inventor
{
    /// <summary>
    /// Stub of a part document. The real ActiveDocument is returned typed as <c>_Document</c>
    /// and cast to this when its kind is part; the component definition holds the parameters.
    /// </summary>
    public class PartDocument : _Document
    {
        public virtual PartComponentDefinition ComponentDefinition => throw Stub.Error();
    }

    /// <summary>Stub of the part component definition (the parametric body of a part).</summary>
    public class PartComponentDefinition
    {
        public virtual Parameters Parameters => throw Stub.Error();
    }

    /// <summary>
    /// Stub of the master Parameters collection. The exporter reads <see cref="UserParameters"/>
    /// (the user-authored named parameters); model parameters (d0, d1…) are feature/sketch
    /// dimensions captured with their owning feature in later milestones.
    /// </summary>
    public class Parameters
    {
        public virtual UserParameters UserParameters => throw Stub.Error();
    }

    /// <summary>
    /// Stub of the user-parameters collection. Inventor collections are 1-based and indexed via
    /// <c>Item</c> (an object index); the exporter iterates by index (robust against flaky COM
    /// enumerators).
    /// </summary>
    public class UserParameters
    {
        public virtual int Count => throw Stub.Error();

        public virtual UserParameter this[object index] => throw Stub.Error();
    }

    /// <summary>
    /// Stub of one user parameter. <see cref="Expression"/> is the editable expression (units
    /// inline, e.g. "40 mm", and may reference other parameters by name, e.g. "Width * 2").
    /// Units is read via <see cref="get_Units"/> to match the real interop's accessor.
    /// </summary>
    public class UserParameter
    {
        public virtual string Name => throw Stub.Error();

        public virtual string Expression => throw Stub.Error();

        public virtual string get_Units() => throw Stub.Error();
    }

    /// <summary>Length/angle unit identifiers, matching UnitsTypeEnum in the real API.</summary>
    public enum UnitsTypeEnum
    {
        kCentimeterLengthUnits = 11268,
        kMillimeterLengthUnits = 11269,
        kMeterLengthUnits = 11270,
        kInchLengthUnits = 11272,
        kFootLengthUnits = 11273,
        kRadianAngleUnits = 11278,
        kDegreeAngleUnits = 11279,
    }

    /// <summary>
    /// Stub of the document units service. <see cref="GetStringFromType"/> turns a unit type into
    /// its expression abbreviation (e.g. kMillimeterLengthUnits → "mm"), the form the Oblikovati
    /// recipe stores.
    /// </summary>
    public class UnitsOfMeasure
    {
        public virtual UnitsTypeEnum LengthUnits => throw Stub.Error();

        public virtual UnitsTypeEnum AngleUnits => throw Stub.Error();

        public virtual string GetStringFromType(UnitsTypeEnum unitsType) => throw Stub.Error();
    }
}
