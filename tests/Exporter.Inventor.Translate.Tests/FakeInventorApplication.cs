// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Named fake of the Inventor <see cref="Application"/> built by subclassing the all-virtual
    /// stub, so the real <c>InventorSessionAdapter</c> can be exercised with no Inventor install.
    /// </summary>
    public sealed class FakeInventorApplication : Application
    {
        private readonly _Document? _active;

        public FakeInventorApplication(_Document? active)
        {
            _active = active;
        }

        public override _Document? ActiveDocument => _active;

        public string? LastStatus { get; private set; }

        public override string StatusBarText
        {
            get => LastStatus ?? string.Empty;
            set => LastStatus = value;
        }
    }

    /// <summary>
    /// Named fake units service mapping the unit types the exporter reads to their expression
    /// abbreviations, matching how the real GetStringFromType behaves for these units.
    /// </summary>
    public sealed class FakeUnitsOfMeasure : UnitsOfMeasure
    {
        private readonly UnitsTypeEnum _length;
        private readonly UnitsTypeEnum _angle;

        public FakeUnitsOfMeasure(
            UnitsTypeEnum length = UnitsTypeEnum.kMillimeterLengthUnits,
            UnitsTypeEnum angle = UnitsTypeEnum.kDegreeAngleUnits)
        {
            _length = length;
            _angle = angle;
        }

        public override UnitsTypeEnum LengthUnits => _length;

        public override UnitsTypeEnum AngleUnits => _angle;

        public override string GetStringFromType(UnitsTypeEnum unitsType) => unitsType switch
        {
            UnitsTypeEnum.kMillimeterLengthUnits => "mm",
            UnitsTypeEnum.kCentimeterLengthUnits => "cm",
            UnitsTypeEnum.kMeterLengthUnits => "m",
            UnitsTypeEnum.kInchLengthUnits => "in",
            UnitsTypeEnum.kFootLengthUnits => "ft",
            UnitsTypeEnum.kRadianAngleUnits => "rad",
            UnitsTypeEnum.kDegreeAngleUnits => "deg",
            _ => "mm",
        };
    }

    /// <summary>Named fake user parameter.</summary>
    public sealed class FakeUserParameter : UserParameter
    {
        public FakeUserParameter(string name, string expression, string units)
        {
            _name = name;
            _expression = expression;
            _units = units;
        }

        private readonly string _name;
        private readonly string _expression;
        private readonly string _units;

        public override string Name => _name;

        public override string Expression => _expression;

        public override string get_Units() => _units;
    }

    /// <summary>Named fake 1-based user-parameters collection backed by a list.</summary>
    public sealed class FakeUserParameters : UserParameters
    {
        private readonly IList<UserParameter> _items;

        public FakeUserParameters(IList<UserParameter> items)
        {
            _items = items;
        }

        public override int Count => _items.Count;

        public override UserParameter this[object index] => _items[(int)index - 1];
    }

    /// <summary>Named fake part component definition holding parameters, sketches, features and work planes.</summary>
    public sealed class FakePartComponentDefinition : PartComponentDefinition
    {
        private readonly Parameters _parameters;
        private readonly PlanarSketches _sketches;
        private readonly PartFeatures _features;
        private readonly WorkPlanes _workPlanes;

        public FakePartComponentDefinition(
            IList<UserParameter> userParameters,
            IList<PlanarSketch>? sketches = null,
            IList<ExtrudeFeature>? extrudes = null,
            IList<WorkPlane>? workPlanes = null)
        {
            _parameters = new FakeParameters(new FakeUserParameters(userParameters));
            _sketches = new FakePlanarSketches(sketches ?? new List<PlanarSketch>());
            _features = new FakePartFeatures(extrudes ?? new List<ExtrudeFeature>());
            _workPlanes = new FakeWorkPlanes(workPlanes ?? new List<WorkPlane>());
        }

        public override Parameters Parameters => _parameters;

        public override PlanarSketches Sketches => _sketches;

        public override PartFeatures Features => _features;

        public override WorkPlanes WorkPlanes => _workPlanes;
    }

    /// <summary>Named fake master Parameters collection exposing the user parameters.</summary>
    public sealed class FakeParameters : Parameters
    {
        private readonly UserParameters _user;

        public FakeParameters(UserParameters user)
        {
            _user = user;
        }

        public override UserParameters UserParameters => _user;
    }

    /// <summary>Named fake part document with units and user parameters.</summary>
    public sealed class FakePartDocument : PartDocument
    {
        private readonly string _displayName;
        private readonly string _fullFileName;
        private readonly UnitsOfMeasure _units;
        private readonly PartComponentDefinition _definition;

        public FakePartDocument(
            string displayName,
            string fullFileName,
            UnitsOfMeasure units,
            IList<UserParameter> userParameters,
            IList<PlanarSketch>? sketches = null,
            IList<ExtrudeFeature>? extrudes = null,
            IList<WorkPlane>? workPlanes = null)
        {
            _displayName = displayName;
            _fullFileName = fullFileName;
            _units = units;
            _definition = new FakePartComponentDefinition(userParameters, sketches, extrudes, workPlanes);
        }

        public override DocumentTypeEnum DocumentType => DocumentTypeEnum.kPartDocumentObject;

        public override string DisplayName => _displayName;

        public override string FullFileName => _fullFileName;

        public override UnitsOfMeasure UnitsOfMeasure => _units;

        public override PartComponentDefinition ComponentDefinition => _definition;
    }

    /// <summary>
    /// Named fake document for the simple kind/units cases (assembly, drawing). Parts use
    /// <see cref="FakePartDocument"/> because the adapter casts a part to <c>PartDocument</c>.
    /// </summary>
    public sealed class FakeInventorDocument : _Document
    {
        private readonly DocumentTypeEnum _type;
        private readonly string _displayName;
        private readonly string _fullFileName;
        private readonly UnitsOfMeasure _units;

        public FakeInventorDocument(
            DocumentTypeEnum type,
            string displayName,
            string fullFileName,
            UnitsOfMeasure? units = null)
        {
            _type = type;
            _displayName = displayName;
            _fullFileName = fullFileName;
            _units = units ?? new FakeUnitsOfMeasure();
        }

        public override DocumentTypeEnum DocumentType => _type;

        public override string DisplayName => _displayName;

        public override string FullFileName => _fullFileName;

        public override UnitsOfMeasure UnitsOfMeasure => _units;
    }
}
