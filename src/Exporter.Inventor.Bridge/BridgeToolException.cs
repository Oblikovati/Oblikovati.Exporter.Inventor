// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace Oblikovati.Exporter.Inventor.Bridge
{
    /// <summary>
    /// Thrown when a bridge tool call fails on the host: either the MCP layer reports a
    /// protocol error, or the tool returns a result flagged <c>isError</c>. Carries the
    /// offending tool name and the host's own message so callers can act on both.
    /// </summary>
    public sealed class BridgeToolException : Exception
    {
        /// <summary>The name of the tool that failed (e.g. <c>documents_save_as</c>).</summary>
        public string ToolName { get; }

        /// <summary>The raw message the host returned for the failure.</summary>
        public string HostMessage { get; }

        public BridgeToolException(string toolName, string hostMessage, Exception? innerException = null)
            : base("Bridge tool '" + toolName + "' failed: " + hostMessage, innerException)
        {
            ToolName = toolName;
            HostMessage = hostMessage;
        }
    }
}
