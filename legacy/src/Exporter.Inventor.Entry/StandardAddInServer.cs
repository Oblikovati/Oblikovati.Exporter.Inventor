// SPDX-License-Identifier: GPL-2.0-only
using System.Runtime.InteropServices;
using Inventor;
using Oblikovati.Exporter.Inventor.Inv;

namespace Oblikovati.Exporter.Inventor.Entry
{
    /// <summary>
    /// The Inventor add-in entry point. Inventor instantiates this via the ClassId in the
    /// .addin manifest and calls <see cref="Activate"/> at load. The ClassId GUID here MUST
    /// match the manifest's &lt;ClassId&gt; (deploy/Oblikovati.Exporter.Inventor.addin).
    ///
    /// At activation it publishes the "Export to Oblikovati" ribbon button (<see cref="RibbonCommand"/>),
    /// whose click calls <see cref="RunExport"/>. All real logic lives in the host-free
    /// <see cref="ExportJob"/>; this class only owns the Inventor session and the command.
    /// </summary>
    [Guid("D29E9B5F-00C3-4F22-B8BA-555610F62927")]
    [ComVisible(true)]
    [ProgId("Oblikovati.Exporter.Inventor")]
    public sealed class StandardAddInServer : ApplicationAddInServer
    {
        private Application? _application;
        private RibbonCommand? _command;

        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            _application = addInSiteObject.Application;
            _command = new RibbonCommand(RunExport);
            _command.Register(_application);
        }

        public void Deactivate()
        {
            _command = null;
            _application = null;
        }

        /// <summary>Legacy hook required by the interface; the add-in uses ribbon commands.</summary>
        public void ExecuteCommand(int commandID)
        {
        }

        /// <summary>No automation object is exposed to other clients.</summary>
        public object Automation => null!;

        /// <summary>
        /// Exports the active document next to its source file and reports the result on the
        /// status bar. Invoked by the ribbon button.
        /// </summary>
        public void RunExport()
        {
            if (_application == null)
            {
                return;
            }
            var session = new InventorSessionAdapter(_application);
            var sink = new DirectoryDocumentSink(session.OutputDirectory());
            string summary = ExportJob.Run(session, sink);
            session.ShowMessage(summary);
        }
    }
}
