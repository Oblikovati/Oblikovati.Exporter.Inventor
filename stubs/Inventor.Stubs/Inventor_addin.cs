// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor add-in surface used by the entry point. Shapes match
// the real Autodesk.Inventor.Interop API; this whole layer is unverified without Inventor.
namespace Inventor
{
    /// <summary>
    /// Stub of the contract every Inventor add-in implements. Inventor calls
    /// <see cref="Activate"/> at load and <see cref="Deactivate"/> at unload.
    /// </summary>
    public interface ApplicationAddInServer
    {
        void Activate(ApplicationAddInSite addInSiteObject, bool firstTime);

        void Deactivate();

        /// <summary>Legacy command hook; obsolete in the real API but still part of it.</summary>
        void ExecuteCommand(int commandID);

        /// <summary>Optional automation object exposed to other clients; may be null.</summary>
        object? Automation { get; }
    }

    /// <summary>Stub of the site object handed to an add-in at activation.</summary>
    public class ApplicationAddInSite
    {
        public virtual Application Application => throw Stub.Error();
    }

    /// <summary>Stub of the Inventor application root.</summary>
    public class Application
    {
        /// <summary>The document currently in front, or null when none is open.</summary>
        public virtual _Document? ActiveDocument => throw Stub.Error();

        /// <summary>Writes a line to Inventor's status bar / transcript.</summary>
        public virtual void StatusBarText(string text) => throw Stub.Error();
    }

    internal static class Stub
    {
        internal static System.InvalidOperationException Error() =>
            new System.InvalidOperationException(
                "Inventor stub member invoked: this assembly is compile-only; run inside " +
                "Inventor with the real Autodesk.Inventor.Interop.dll");
    }
}
