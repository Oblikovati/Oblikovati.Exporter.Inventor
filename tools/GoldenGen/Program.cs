// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using System.IO;
using Oblikovati.Exporter.Inventor.Fixtures;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;
using Oblikovati.Exporter.Inventor.Translate;

namespace Oblikovati.Exporter.Inventor.GoldenGen
{
    /// <summary>
    /// Writes golden documents to the directory given as the first argument. Fixtures are the
    /// shared <see cref="InventorSampleParts"/> so the round-trip opens the exact inputs the
    /// unit tests assert, with the real oblikovati-cli — binding the C# emitter to the Go
    /// reader and catching schema drift.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("usage: GoldenGen <output-dir>");
                return 2;
            }

            string outDir = args[0];
            Directory.CreateDirectory(outDir);

            var translator = new DocumentTranslator();
            var writer = new RecipeYamlWriter();
            foreach (InventorDocument fixture in Fixtures())
            {
                OblikovatiDocument recipe = translator.Translate(fixture, new ExportReport());
                string path = Path.Combine(outDir, fixture.DisplayName + Extension(fixture.Kind));
                File.WriteAllText(path, writer.Write(recipe));
                Console.WriteLine("wrote " + path);
            }

            return 0;
        }

        private static string Extension(InventorDocumentKind kind) =>
            kind == InventorDocumentKind.Assembly ? ".oad" : ".opd";

        private static IEnumerable<InventorDocument> Fixtures()
        {
            yield return InventorSampleParts.EmptyPart();
            yield return InventorSampleParts.ParametricPart();
            yield return InventorSampleParts.RectanglePart();
            yield return InventorSampleParts.CirclePart();
        }
    }
}
