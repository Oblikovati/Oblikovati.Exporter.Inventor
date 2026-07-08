// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace Oblikovati.Exporter.Inventor.Bridge
{
    /// <summary>
    /// Connection settings for <see cref="BridgeClient"/>. Defaults target a locally-running
    /// Oblikovati MCP bridge (the in-process add-in served over streamable HTTP at
    /// <c>127.0.0.1:7800/mcp</c>).
    /// </summary>
    public sealed class BridgeClientOptions
    {
        /// <summary>The bridge's streamable-HTTP MCP endpoint used when none is supplied.</summary>
        public const string DefaultEndpoint = "http://127.0.0.1:7800/mcp";

        /// <summary>The MCP endpoint to connect to. Defaults to <see cref="DefaultEndpoint"/>.</summary>
        public Uri Endpoint { get; set; } = new Uri(DefaultEndpoint);

        /// <summary>Client name advertised to the host during the MCP handshake.</summary>
        public string ClientName { get; set; } = "Oblikovati.Exporter.Inventor.Bridge";

        /// <summary>How long the transport waits for the initial connection before failing.</summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
