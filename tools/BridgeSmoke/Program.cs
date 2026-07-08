// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Bridge;

namespace Oblikovati.Exporter.Inventor.Tools.BridgeSmoke
{
    // Drives the proven round trip end to end through the C# BridgeClient against a LIVE
    // Oblikovati MCP bridge, printing each call's JSON result and asserting the extruded
    // 40x30mm x 50mm part measures exactly 60 cm3. This is the B1 acceptance test; it needs
    // a bridge listening on the endpoint (default 127.0.0.1:7800/mcp) and is not run in CI.
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            string endpoint = args.Length > 0 ? args[0] : BridgeClientOptions.DefaultEndpoint;
            string savePath = args.Length > 1 ? args[1] : "csharp_smoke.obk";
            string docName = "csharp_smoke_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);

            var options = new BridgeClientOptions { Endpoint = new Uri(endpoint) };
            Console.WriteLine("Connecting to " + endpoint + " ...");
            await using BridgeClient bridge = await BridgeClient.ConnectAsync(options).ConfigureAwait(false);

            IReadOnlyList<string> tools = await bridge.ListToolNamesAsync().ConfigureAwait(false);
            Console.WriteLine("Connected. Host exposes " + tools.Count + " tools.");

            JsonElement created = await Step(bridge, "create_document",
                new Dictionary<string, object?> { ["type"] = "part", ["name"] = docName });
            long documentId = ReadDocumentId(created);
            Console.WriteLine("  -> document id = " + documentId);

            await Step(bridge, "create_sketch",
                new Dictionary<string, object?> { ["plane"] = "XY" });

            await Step(bridge, "sketch_rectangle",
                new Dictionary<string, object?> { ["sketchIndex"] = 0, ["width"] = "40 mm", ["height"] = "30 mm" });

            await Step(bridge, "add_feature", new Dictionary<string, object?>
            {
                ["kind"] = "extrude",
                ["args"] = new Dictionary<string, object?>
                {
                    ["sketchIndex"] = 0,
                    ["profileIndex"] = 0,
                    ["distance"] = "50 mm",
                    ["operation"] = "new",
                },
            });

            JsonElement props = await Step(bridge, "get_physical_properties", null);
            double volume = ReadVolume(props);
            Console.WriteLine("  -> volume = " + volume.ToString("0.####", CultureInfo.InvariantCulture) + " cm3");

            await Step(bridge, "documents_save_as", new Dictionary<string, object?>
            {
                ["document"] = documentId,
                ["newFullDocumentName"] = savePath,
            });
            Console.WriteLine("Saved to " + savePath);

            const double expected = 60.0;
            bool ok = Math.Abs(volume - expected) < 1e-6;
            Console.WriteLine();
            Console.WriteLine(ok
                ? "PASS: volume == " + expected.ToString(CultureInfo.InvariantCulture) + " cm3"
                : "FAIL: volume " + volume.ToString(CultureInfo.InvariantCulture) + " != " + expected.ToString(CultureInfo.InvariantCulture));
            return ok ? 0 : 1;
        }

        private static async Task<JsonElement> Step(BridgeClient bridge, string tool, IReadOnlyDictionary<string, object?>? args)
        {
            Console.WriteLine("CALL " + tool);
            JsonElement result = await bridge.CallToolAsync(tool, args).ConfigureAwait(false);
            Console.WriteLine("  " + Compact(result));
            return result;
        }

        private static string Compact(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Undefined)
                return "(no content)";
            return JsonSerializer.Serialize(element);
        }

        // The bridge returns the created document's numeric id; accept the common field spellings.
        private static long ReadDocumentId(JsonElement created)
        {
            foreach (string name in new[] { "document", "documentId", "id" })
            {
                if (created.ValueKind == JsonValueKind.Object &&
                    created.TryGetProperty(name, out JsonElement value) &&
                    value.ValueKind == JsonValueKind.Number &&
                    value.TryGetInt64(out long id))
                {
                    return id;
                }
            }
            throw new InvalidOperationException("create_document did not return a numeric document id: " + Compact(created));
        }

        private static double ReadVolume(JsonElement props)
        {
            if (PhysicalProperties.TryReadVolumeCm3(props, out double volume))
                return volume;
            throw new InvalidOperationException("get_physical_properties did not return a volume: " + Compact(props));
        }
    }
}
