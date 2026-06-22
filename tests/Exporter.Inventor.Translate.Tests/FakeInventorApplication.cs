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

    /// <summary>Named fake parameter.</summary>
    public sealed class FakeParameter : Parameter
    {
        public FakeParameter(string name, string expression, string units)
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

        public override string Units => _units;
    }

    /// <summary>Named fake 1-based user-parameters collection backed by a list.</summary>
    public sealed class FakeUserParameters : UserParameters
    {
        private readonly IList<Parameter> _items;

        public FakeUserParameters(IList<Parameter> items)
        {
            _items = items;
        }

        public override int Count => _items.Count;

        public override Parameter this[int index] => _items[index - 1];
    }

    /// <summary>Named fake part component definition holding the user parameters.</summary>
    public sealed class FakePartComponentDefinition : PartComponentDefinition
    {
        private readonly Parameters _parameters;

        public FakePartComponentDefinition(IList<Parameter> userParameters)
        {
            _parameters = new FakeParameters(new FakeUserParameters(userParameters));
        }

        public override Parameters Parameters => _parameters;
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
            IList<Parameter> userParameters)
        {
            _displayName = displayName;
            _fullFileName = fullFileName;
            _units = units;
            _definition = new FakePartComponentDefinition(userParameters);
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
