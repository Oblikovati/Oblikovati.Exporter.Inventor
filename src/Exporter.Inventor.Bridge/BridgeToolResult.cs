// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;
using System.Text.Json;

namespace Oblikovati.Exporter.Inventor.Bridge
{
    /// <summary>
    /// Pure selection of a single JSON result out of an MCP tool response, decoupled from the
    /// protocol types so it is unit-testable with plain values. The host returns a tool result
    /// as either structured JSON content or one-or-more text blocks; an <c>isError</c> response
    /// is surfaced as a <see cref="BridgeToolException"/> carrying the host's message.
    /// </summary>
    internal static class BridgeToolResult
    {
        /// <summary>
        /// Picks the result element: structured content when present, otherwise the joined text
        /// blocks parsed as JSON (falling back to a JSON string when the text is not JSON). A
        /// content-less success returns an <see cref="JsonValueKind.Undefined"/> element.
        /// </summary>
        public static JsonElement Select(string toolName, JsonElement? structuredContent, IReadOnlyList<string> textBlocks, bool isError)
        {
            string joined = JoinText(textBlocks);
            if (isError)
                throw new BridgeToolException(toolName, joined.Length == 0 ? "(no host message)" : joined);

            if (structuredContent.HasValue &&
                structuredContent.Value.ValueKind != JsonValueKind.Undefined &&
                structuredContent.Value.ValueKind != JsonValueKind.Null)
            {
                return structuredContent.Value.Clone();
            }

            return joined.Length == 0 ? default : ParseOrWrap(joined);
        }

        private static string JoinText(IReadOnlyList<string> textBlocks)
        {
            var parts = new List<string>(textBlocks.Count);
            foreach (string block in textBlocks)
            {
                if (!string.IsNullOrEmpty(block))
                    parts.Add(block);
            }
            return string.Join("\n", parts);
        }

        private static JsonElement ParseOrWrap(string text)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(text);
                return doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // Not JSON — surface the raw host text as a JSON string element.
                using JsonDocument doc = JsonDocument.Parse(JsonSerializer.Serialize(text));
                return doc.RootElement.Clone();
            }
        }
    }
}
