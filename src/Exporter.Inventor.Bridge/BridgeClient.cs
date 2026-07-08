// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Oblikovati.Exporter.Inventor.Bridge
{
    /// <summary>
    /// A thin, typed wrapper over the official ModelContextProtocol client, connected to the
    /// Oblikovati MCP bridge over streamable HTTP. It owns a single MCP session: call tools by
    /// name with a JSON-shaped argument dictionary and get the tool's JSON result back, or a
    /// <see cref="BridgeToolException"/> on failure.
    /// </summary>
    /// <example>
    /// <code>
    /// await using var bridge = await BridgeClient.ConnectAsync();
    /// JsonElement doc = await bridge.CallToolAsync("create_document",
    ///     new Dictionary&lt;string, object?&gt; { ["type"] = "part", ["name"] = "spike" });
    /// </code>
    /// </example>
    public sealed class BridgeClient : IAsyncDisposable
    {
        private readonly McpClient _client;
        private readonly HttpClientTransport _transport;

        private BridgeClient(McpClient client, HttpClientTransport transport)
        {
            _client = client;
            _transport = transport;
        }

        /// <summary>The endpoint this client is connected to.</summary>
        public Uri Endpoint { get; private set; } = new Uri(BridgeClientOptions.DefaultEndpoint);

        /// <summary>Connects to the bridge and completes the MCP handshake.</summary>
        public static async Task<BridgeClient> ConnectAsync(BridgeClientOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new BridgeClientOptions();
            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = options.Endpoint,
                TransportMode = HttpTransportMode.StreamableHttp,
                Name = options.ClientName,
                ConnectionTimeout = options.ConnectionTimeout,
            });
            McpClient client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new BridgeClient(client, transport) { Endpoint = options.Endpoint };
        }

        /// <summary>Lists the names of every tool the bridge exposes.</summary>
        public async Task<IReadOnlyList<string>> ListToolNamesAsync(CancellationToken cancellationToken = default)
        {
            IList<McpClientTool> tools = await _client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var names = new List<string>(tools.Count);
            foreach (McpClientTool tool in tools)
                names.Add(tool.Name);
            return names;
        }

        /// <summary>
        /// Calls a bridge tool and returns its JSON result. <paramref name="arguments"/> may nest
        /// dictionaries/lists — the values are serialized to JSON by the MCP client. A host-side
        /// failure is raised as a <see cref="BridgeToolException"/> naming the tool.
        /// </summary>
        public async Task<JsonElement> CallToolAsync(string tool, IReadOnlyDictionary<string, object?>? arguments = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tool))
                throw new ArgumentException("Tool name must be non-empty.", nameof(tool));

            CallToolResult result;
            try
            {
                result = await _client.CallToolAsync(tool, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (McpException ex)
            {
                throw new BridgeToolException(tool, ex.Message, ex);
            }

            return BridgeToolResult.Select(tool, result.StructuredContent, TextBlocksOf(result), result.IsError ?? false);
        }

        private static IReadOnlyList<string> TextBlocksOf(CallToolResult result)
        {
            var blocks = new List<string>();
            if (result.Content == null)
                return blocks;
            foreach (ContentBlock block in result.Content)
            {
                if (block is TextContentBlock text && text.Text != null)
                    blocks.Add(text.Text);
            }
            return blocks;
        }

        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync().ConfigureAwait(false);
            await _transport.DisposeAsync().ConfigureAwait(false);
        }
    }
}
