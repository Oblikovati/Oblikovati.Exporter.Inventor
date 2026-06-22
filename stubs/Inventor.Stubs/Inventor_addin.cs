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

        /// <summary>Optional automation object exposed to other clients; null when none.</summary>
        object Automation { get; }
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

        /// <summary>The application status-bar text. Assigning shows a message to the user.</summary>
        public virtual string StatusBarText
        {
            get => throw Stub.Error();
            set => throw Stub.Error();
        }

        /// <summary>Entry to control definitions (button commands).</summary>
        public virtual CommandManager CommandManager => throw Stub.Error();

        /// <summary>Entry to the ribbons (where the add-in publishes its button).</summary>
        public virtual UserInterfaceManager UserInterfaceManager => throw Stub.Error();
    }

    internal static class Stub
    {
        internal static System.InvalidOperationException Error() =>
            new System.InvalidOperationException(
                "Inventor stub member invoked: this assembly is compile-only; run inside " +
                "Inventor with the real Autodesk.Inventor.Interop.dll");
    }
}
