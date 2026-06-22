// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Named fakes of the Inventor assembly surface, subclassing the all-virtual stub so the real
    /// <c>ComponentExtractor</c> + adapter recursion run with no Inventor install.
    /// </summary>
    public sealed class FakeAssemblyDocument : AssemblyDocument
    {
        private readonly string _displayName;
        private readonly string _fullFileName;
        private readonly UnitsOfMeasure _units;
        private readonly AssemblyComponentDefinition _definition;

        public FakeAssemblyDocument(
            string displayName, string fullFileName, IList<ComponentOccurrence>? occurrences = null)
        {
            _displayName = displayName;
            _fullFileName = fullFileName;
            _units = new FakeUnitsOfMeasure();
            _definition = new FakeAssemblyComponentDefinition(occurrences ?? new List<ComponentOccurrence>());
        }

        public override DocumentTypeEnum DocumentType => DocumentTypeEnum.kAssemblyDocumentObject;

        public override string DisplayName => _displayName;

        public override string FullFileName => _fullFileName;

        public override UnitsOfMeasure UnitsOfMeasure => _units;

        public override AssemblyComponentDefinition ComponentDefinition => _definition;
    }

    public sealed class FakeAssemblyComponentDefinition : AssemblyComponentDefinition
    {
        private readonly ComponentOccurrences _occurrences;

        public FakeAssemblyComponentDefinition(IList<ComponentOccurrence> occurrences)
        {
            _occurrences = new FakeComponentOccurrences(occurrences);
        }

        public override ComponentOccurrences Occurrences => _occurrences;
    }

    public sealed class FakeComponentOccurrences : ComponentOccurrences
    {
        private readonly IList<ComponentOccurrence> _items;

        public FakeComponentOccurrences(IList<ComponentOccurrence> items)
        {
            _items = items;
        }

        public override int Count => _items.Count;

        public override ComponentOccurrence this[int index] => _items[index - 1];
    }

    public sealed class FakeComponentOccurrence : ComponentOccurrence
    {
        private readonly string _name;
        private readonly ComponentDefinition _definition;
        private readonly Matrix _transform;

        public FakeComponentOccurrence(string name, _Document referenced, double[] position)
        {
            _name = name;
            _definition = new FakeComponentDefinition(referenced);
            _transform = new FakeMatrix(position);
        }

        public override string Name => _name;

        public override ComponentDefinition Definition => _definition;

        public override Matrix Transformation => _transform;
    }

    public sealed class FakeComponentDefinition : ComponentDefinition
    {
        private readonly _Document _document;

        public FakeComponentDefinition(_Document document)
        {
            _document = document;
        }

        public override object Document => _document;
    }

    /// <summary>An identity-rotation transform with the given translation (cm), read via get_Cell.</summary>
    public sealed class FakeMatrix : Matrix
    {
        private readonly double[] _position;

        public FakeMatrix(double[] position)
        {
            _position = position;
        }

        public override double get_Cell(int row, int col)
        {
            if (col == 4)
            {
                return row == 4 ? 1.0 : _position[row - 1];
            }
            if (row == 4)
            {
                return 0.0;
            }
            return row == col ? 1.0 : 0.0; // identity rotation
        }
    }
}
