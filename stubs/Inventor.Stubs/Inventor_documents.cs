// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor document read surface the adapter walks.
namespace Inventor
{
    /// <summary>
    /// Document kinds, matching DocumentTypeEnum in the real API (values are the genuine
    /// constants so equality checks behave the same against stub or real interop).
    /// </summary>
    public enum DocumentTypeEnum
    {
        kUnknownDocumentObject = 11484,
        kPartDocumentObject = 12290,
        kAssemblyDocumentObject = 12291,
        kDrawingDocumentObject = 12292,
        kPresentationDocumentObject = 12293,
    }

    /// <summary>
    /// Stub of the common document interface (the real type is named <c>_Document</c>).
    /// The adapter reads these to decide part vs assembly and to name the output.
    /// </summary>
    public class _Document
    {
        public virtual DocumentTypeEnum DocumentType => throw Stub.Error();

        public virtual string DisplayName => throw Stub.Error();

        public virtual string FullFileName => throw Stub.Error();
    }
}
