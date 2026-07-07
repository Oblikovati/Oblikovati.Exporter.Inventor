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
                // A suppressed occurrence carries no resolvable Definition (its Definition/Document
                // getter raises E_FAIL) and contributes no geometry, so skip it rather than aborting.
                if (occurrence.Suppressed)
                {
                    continue;
                }

                InventorOccurrence? entry = TryReadOccurrence(occurrence, resolve);
                if (entry != null)
                {
                    ir.Occurrences.Add(entry);
                }
            }
        }

        // Reads one occurrence, tolerating an unresolved reference whose Definition/Document raises
        // E_FAIL (missing file, broken link): returns null so the caller skips just that component.
        private static InventorOccurrence? TryReadOccurrence(
            ComponentOccurrence occurrence, Func<_Document, InventorDocument> resolve)
        {
            try
            {
                var child = (_Document)occurrence.Definition.Document;
                Matrix m = occurrence.Transformation;
                return new InventorOccurrence
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
                };
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return null;
            }
        }
    }
}
