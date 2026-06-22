// SPDX-License-Identifier: GPL-2.0-only
using System;
using Inventor;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Inv
{
    /// <summary>
    /// Reads an assembly's placed components into the IR. Each occurrence's referenced document
    /// is resolved (and de-duplicated) through <paramref name="resolve"/> — supplied by the
    /// adapter so the same recursion that extracts the active document extracts its components —
    /// and its placement Matrix becomes a position (cm) plus a row-major 3x3 rotation. The
    /// transform is read cell-by-cell (1-based row/col) so it does not depend on the array layout.
    /// </summary>
    public static class ComponentExtractor
    {
        public static void Extract(
            AssemblyComponentDefinition definition, InventorDocument ir, Func<_Document, InventorDocument> resolve)
        {
            ComponentOccurrences occurrences = definition.Occurrences;
            for (int i = 1; i <= occurrences.Count; i++)
            {
                ComponentOccurrence occurrence = occurrences[i];
                var child = (_Document)occurrence.Definition.Document;
                Matrix m = occurrence.Transformation;
                ir.Occurrences.Add(new InventorOccurrence
                {
                    Name = occurrence.Name,
                    Component = resolve(child),
                    Position = new[] { m.get_Cell(1, 4), m.get_Cell(2, 4), m.get_Cell(3, 4) },
                    Rotation = new[]
                    {
                        m.get_Cell(1, 1), m.get_Cell(1, 2), m.get_Cell(1, 3),
                        m.get_Cell(2, 1), m.get_Cell(2, 2), m.get_Cell(2, 3),
                        m.get_Cell(3, 1), m.get_Cell(3, 2), m.get_Cell(3, 3),
                    },
                });
            }
        }
    }
}
