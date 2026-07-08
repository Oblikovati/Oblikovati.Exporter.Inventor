// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the Inventor ribbon/command surface the add-in uses to publish its
// button. Shapes mirror the genuine Autodesk.Inventor.Interop; the OnExecute event delegate
// matches ButtonDefinitionSink_OnExecuteEventHandler so a void(NameValueMap) handler binds
// against both this stub and the real assembly.
namespace Inventor
{
    /// <summary>The execute-event delegate for a button (Context carries the invocation data).</summary>
    public delegate void ButtonDefinitionSink_OnExecuteEventHandler(NameValueMap Context);

    /// <summary>Stub of the name/value map handed to a command's OnExecute.</summary>
    public class NameValueMap
    {
    }

    /// <summary>Command classification (only the value the add-in passes is needed).</summary>
    public enum CommandTypesEnum
    {
        kShapeEditCmdType = 1,
        kQueryOnlyCmdType = 2,
        kNonShapeEditCmdType = 32,
    }

    /// <summary>How a ribbon button shows text/icon.</summary>
    public enum ButtonDisplayEnum
    {
        kNoTextWithIcon = 43009,
        kDisplayTextInLearningMode = 43011,
    }

    /// <summary>Stub of a button command definition; OnExecute fires when the user clicks it.</summary>
    public class ButtonDefinition
    {
        public virtual event ButtonDefinitionSink_OnExecuteEventHandler OnExecute
        {
            add => throw Stub.Error();
            remove => throw Stub.Error();
        }
    }

    /// <summary>Stub of the control-definitions collection (creates button definitions).</summary>
    public class ControlDefinitions
    {
        public virtual ButtonDefinition AddButtonDefinition(
            string displayName,
            string internalName,
            CommandTypesEnum classification,
            object? clientId = null,
            string? descriptionText = "",
            string? toolTipText = "",
            object? standardIcon = null,
            object? largeIcon = null,
            ButtonDisplayEnum buttonDisplay = ButtonDisplayEnum.kDisplayTextInLearningMode) =>
            throw Stub.Error();
    }

    /// <summary>Stub of the command manager (entry to control definitions).</summary>
    public class CommandManager
    {
        public virtual ControlDefinitions ControlDefinitions => throw Stub.Error();
    }

    /// <summary>Stub of the UI manager (entry to the ribbons).</summary>
    public class UserInterfaceManager
    {
        public virtual Ribbons Ribbons => throw Stub.Error();
    }

    /// <summary>Stub of the ribbons collection (indexed by environment name, e.g. "Part").</summary>
    public class Ribbons
    {
        public virtual Ribbon this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one ribbon.</summary>
    public class Ribbon
    {
        public virtual RibbonTabs RibbonTabs => throw Stub.Error();
    }

    /// <summary>Stub of the ribbon-tabs collection (indexed by internal name, e.g. "id_TabTools").</summary>
    public class RibbonTabs
    {
        public virtual RibbonTab this[object index] => throw Stub.Error();
    }

    /// <summary>Stub of one ribbon tab.</summary>
    public class RibbonTab
    {
        public virtual RibbonPanels RibbonPanels => throw Stub.Error();
    }

    /// <summary>Stub of a ribbon-panels collection (the add-in adds its own panel).</summary>
    public class RibbonPanels
    {
        public virtual RibbonPanel Add(
            string displayName, string internalName, string clientId,
            string? targetPanelInternalName = "", bool? insertBeforeTargetPanel = false) =>
            throw Stub.Error();
    }

    /// <summary>Stub of one ribbon panel.</summary>
    public class RibbonPanel
    {
        public virtual CommandControls CommandControls => throw Stub.Error();
    }

    /// <summary>Stub of a panel's command controls (the add-in adds its button here).</summary>
    public class CommandControls
    {
        public virtual CommandControl AddButton(
            ButtonDefinition buttonDefinition, bool? useLargeIcon = false, bool? showText = true,
            string? targetControlInternalName = "", bool? insertBeforeTargetControl = false) =>
            throw Stub.Error();
    }

    /// <summary>Stub of a placed command control.</summary>
    public class CommandControl
    {
    }
}
