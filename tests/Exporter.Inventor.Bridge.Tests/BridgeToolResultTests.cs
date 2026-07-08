// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Text.Json;
using Oblikovati.Exporter.Inventor.Bridge;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Bridge.Tests
{
    public sealed class BridgeToolResultTests
    {
        private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

        [Fact]
        public void Prefers_structured_content_when_present()
        {
            JsonElement result = BridgeToolResult.Select(
                "get_physical_properties",
                Json("{\"volume\":60}"),
                new[] { "ignored text" },
                isError: false);

            Assert.Equal(JsonValueKind.Object, result.ValueKind);
            Assert.Equal(60, result.GetProperty("volume").GetInt32());
        }

        [Fact]
        public void Falls_back_to_text_block_parsed_as_json()
        {
            JsonElement result = BridgeToolResult.Select(
                "create_document",
                structuredContent: null,
                new[] { "{\"id\":1}" },
                isError: false);

            Assert.Equal(1, result.GetProperty("id").GetInt32());
        }

        [Fact]
        public void Wraps_non_json_text_as_a_json_string()
        {
            JsonElement result = BridgeToolResult.Select(
                "some_tool",
                structuredContent: null,
                new[] { "plain host text" },
                isError: false);

            Assert.Equal(JsonValueKind.String, result.ValueKind);
            Assert.Equal("plain host text", result.GetString());
        }

        [Fact]
        public void Empty_success_returns_an_undefined_element()
        {
            JsonElement result = BridgeToolResult.Select(
                "documents_save_as",
                structuredContent: null,
                Array.Empty<string>(),
                isError: false);

            Assert.Equal(JsonValueKind.Undefined, result.ValueKind);
        }

        [Fact]
        public void Error_throws_with_tool_name_and_host_message()
        {
            var ex = Assert.Throws<BridgeToolException>(() => BridgeToolResult.Select(
                "documents_save_as",
                structuredContent: null,
                new[] { "no store configured" },
                isError: true));

            Assert.Equal("documents_save_as", ex.ToolName);
            Assert.Equal("no store configured", ex.HostMessage);
            Assert.Contains("documents_save_as", ex.Message);
            Assert.Contains("no store configured", ex.Message);
        }

        [Fact]
        public void Error_without_text_reports_a_placeholder_message()
        {
            var ex = Assert.Throws<BridgeToolException>(() => BridgeToolResult.Select(
                "add_feature",
                structuredContent: null,
                Array.Empty<string>(),
                isError: true));

            Assert.Equal("add_feature", ex.ToolName);
            Assert.False(string.IsNullOrWhiteSpace(ex.HostMessage));
        }

        [Fact]
        public void Joins_multiple_text_blocks_into_the_error_message()
        {
            var ex = Assert.Throws<BridgeToolException>(() => BridgeToolResult.Select(
                "add_feature",
                structuredContent: null,
                new[] { "line one", "line two" },
                isError: true));

            Assert.Contains("line one", ex.HostMessage);
            Assert.Contains("line two", ex.HostMessage);
        }
    }
}
