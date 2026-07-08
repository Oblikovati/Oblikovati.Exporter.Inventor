// SPDX-License-Identifier: GPL-2.0-only
namespace Oblikovati.Exporter.Inventor.Translate
{
    /// <summary>
    /// Hands out monotonically increasing document-local ids. Sketches, points and entities
    /// share one id space (matching the Go codec, where a sketch's id precedes its points'
    /// and entities' ids), so one allocator threads through the document.
    /// </summary>
    public sealed class IdAllocator
    {
        private int _next;

        public IdAllocator(int start = 1)
        {
            _next = start;
        }

        public int Next() => _next++;
    }
}
