// SPDX-License-Identifier: GPL-2.0-only
using System;
using Inventor;

namespace Oblikovati.Exporter.Inventor.Entry
{
    /// <summary>
    /// Publishes the "Export to Oblikovati" button on the Tools tab of the Part and Assembly
    /// ribbons and routes its click to the export. The add-in shell (<see cref="StandardAddInServer"/>)
    /// constructs this at activation with the export action; all Inventor UI calls live here.
    /// </summary>
    public sealed class RibbonCommand
    {
        // Must match StandardAddInServer's [Guid] / the .addin ClientId.
        private const string ClientId = "{D29E9B5F-00C3-4F22-B8BA-555610F62927}";

        private readonly Action _runExport;
        private ButtonDefinition? _button;

        public RibbonCommand(Action runExport)
        {
            _runExport = runExport;
        }

        /// <summary>Creates the button definition and places it on the Part and Assembly ribbons.</summary>
        public void Register(Application application)
        {
            _button = application.CommandManager.ControlDefinitions.AddButtonDefinition(
                "Export to Oblikovati",
                "OblikovatiExport",
                CommandTypesEnum.kNonShapeEditCmdType,
                ClientId,
                "Export the active document to a native Oblikovati .opd/.oad file, keeping the parametric history.",
                "Export to Oblikovati");
            _button.OnExecute += OnExecute;

            AddToRibbon(application, "Part", _button);
            AddToRibbon(application, "Assembly", _button);
        }

        private static void AddToRibbon(Application application, string ribbonName, ButtonDefinition button)
        {
            RibbonPanel panel = application.UserInterfaceManager.Ribbons[ribbonName]
                .RibbonTabs["id_TabTools"]
                .RibbonPanels.Add("Oblikovati", "OblikovatiPanel_" + ribbonName, ClientId);
            panel.CommandControls.AddButton(button, true);
        }

        private void OnExecute(NameValueMap context) => _runExport();
    }
}
