// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor assembly read surface the adapter walks. Shapes mirror
// the genuine Autodesk.Inventor.Interop (verified by compiling the adapter against the real
// assembly): the occurrences collection is 1-based and int-indexed, an occurrence's referenced
// document is reached through Definition.Document (typed object), and its placement is a Matrix
// read cell-by-cell (1-based row/col) to stay layout-agnostic.
namespace Inventor
{
    /// <summary>Stub of an assembly document.</summary>
    public class AssemblyDocument : _Document
    {
        public virtual AssemblyComponentDefinition ComponentDefinition => throw Stub.Error();
    }

    /// <summary>Stub of the assembly component definition (holds the placed components).</summary>
    public class AssemblyComponentDefinition
    {
        public virtual ComponentOccurrences Occurrences => throw Stub.Error();
    }

    /// <summary>Stub of the component-occurrences collection (int-indexed, 1-based).</summary>
    public class ComponentOccurrences
    {
        public virtual int Count => throw Stub.Error();

        public virtual ComponentOccurrence this[int index] => throw Stub.Error();
    }

    /// <summary>Stub of one placed component: its name, referenced definition, and transform.</summary>
    public class ComponentOccurrence
    {
        public virtual string Name => throw Stub.Error();

        public virtual ComponentDefinition Definition => throw Stub.Error();

        public virtual Matrix Transformation => throw Stub.Error();
    }

    /// <summary>Stub of a component definition; its Document is the referenced part/assembly.</summary>
    public class ComponentDefinition
    {
        /// <summary>The referenced document (a PartDocument or AssemblyDocument), typed object.</summary>
        public virtual object Document => throw Stub.Error();
    }

    /// <summary>
    /// Stub of a 4x4 transform. <see cref="get_Cell"/> reads one cell by 1-based row/col (the
    /// real Cell is a parameterized COM property), so callers stay independent of the array
    /// layout: rotation is cells (1..3, 1..3); translation is (1..3, 4).
    /// </summary>
    public class Matrix
    {
        public virtual double get_Cell(int row, int col) => throw Stub.Error();
    }
}
