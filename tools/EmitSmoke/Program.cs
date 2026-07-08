// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Oblikovati.Exporter.Inventor.Bridge;
using Oblikovati.Exporter.Inventor.Emit;
using Oblikovati.Exporter.Inventor.Fixtures;
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Tools.EmitSmoke
{
    // Drives the B2 feature emitter end to end against a LIVE Oblikovati MCP bridge: it emits two
    // Model IR fixtures through DocumentEmitter and asserts each measured volume. Needs a bridge
    // listening on the endpoint (default 127.0.0.1:7800/mcp); it is not run in CI.
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            string endpoint = args.Length > 0 ? args[0] : BridgeClientOptions.DefaultEndpoint;
            var options = new BridgeClientOptions { Endpoint = new Uri(endpoint) };
            Console.WriteLine("Connecting to " + endpoint + " ...");
            await using BridgeClient bridge = await BridgeClient.ConnectAsync(options).ConfigureAwait(false);
            IReadOnlyList<string> tools = await bridge.ListToolNamesAsync().ConfigureAwait(false);
            Console.WriteLine("Connected. Host exposes " + tools.Count + " tools.");

            // Fixture 1 is exact. Fixture 2's analytic box−cylinder is 60 − π·0.8²·5 = 60 − 3.2π;
            // the host tessellates the circular hole into a 24-gon, so the measured solid carries a
            // small deterministic excess (a chorded hole removes slightly less material). Assert
            // against the analytic value within a faceting tolerance.
            bool ok = true;
            ok &= await Fixture(bridge, "box", InventorSampleParts.BoxPart(), 60.0, 1e-3);
            ok &= await Fixture(bridge, "box-with-hole", InventorSampleParts.BoxWithHolePart(),
                60.0 - (Math.PI * 0.8 * 0.8 * 5.0), 0.2);

            Console.WriteLine();
            Console.WriteLine(ok ? "PASS: both fixtures matched expected volume." : "FAIL: a fixture volume did not match.");
            return ok ? 0 : 1;
        }

        private static async Task<bool> Fixture(BridgeClient bridge, string label, InventorDocument doc, double expected, double tolerance)
        {
            Console.WriteLine();
            Console.WriteLine("=== fixture: " + label + " ===");
            EmitReport report = await new DocumentEmitter().EmitAsync(bridge, doc).ConfigureAwait(false);
            Console.WriteLine("  document: " + report.DocumentName + " (id " + report.DocumentId + ")");
            Console.WriteLine("  sketches emitted: " + report.SketchesEmitted + ", features emitted: " + report.FeaturesEmitted);
            foreach (string d in report.Deferrals)
                Console.WriteLine("  DEFERRED: " + d);

            var props = await bridge.CallToolAsync("get_physical_properties").ConfigureAwait(false);
            if (!PhysicalProperties.TryReadVolumeCm3(props, out double volume))
            {
                Console.WriteLine("  FAIL: no volume in get_physical_properties result: " + props);
                return false;
            }
            bool pass = Math.Abs(volume - expected) < tolerance;
            Console.WriteLine("  volume = " + Fmt(volume) + " cm3 (expected " + Fmt(expected) + ") => " + (pass ? "PASS" : "FAIL"));
            return pass;
        }

        private static string Fmt(double v) => v.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
