// SPDX-License-Identifier: GPL-2.0-only
using System.Linq;
using Oblikovati.Exporter.Inventor.Fixtures;
using Oblikovati.Exporter.Inventor.Model;
using Oblikovati.Exporter.Inventor.Recipe;
using Oblikovati.Exporter.Inventor.Translate;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    public sealed class DressUpTranslatorTests
    {
        private static FeatureData TranslateLastFeature(InventorDocument doc)
        {
            var part = (PartRecipe)new DocumentTranslator().Translate(doc, new ExportReport()).Model!;
            return part.Features.Last();
        }

        [Fact]
        public void Fillet_emits_value_and_a_geometric_edge_descriptor()
        {
            FeatureData f = TranslateLastFeature(InventorSampleParts.FilletedBoxPart());

            Assert.Equal("fillet", f.Kind);
            EdgeDressData dress = Assert.IsType<EdgeDressData>(f.Fillet!);
            Assert.Equal(0.5, dress.Value);
            GeomEdgeRefData edge = Assert.Single(dress.GeomEdges);
            Assert.Equal(new double[] { 4, 3, 2.5 }, edge.Midpoint);
            Assert.Equal(new double[] { 0, 0, 1 }, edge.Direction);
        }

        [Fact]
        public void Chamfer_emits_value_and_an_edge_descriptor()
        {
            FeatureData f = TranslateLastFeature(InventorSampleParts.ChamferedBoxPart());

            Assert.Equal("chamfer", f.Kind);
            Assert.Equal(0.5, Assert.IsType<EdgeDressData>(f.Chamfer!).Value);
            Assert.Single(f.Chamfer!.GeomEdges);
        }

        [Fact]
        public void Shell_emits_thickness_and_a_removed_face_descriptor()
        {
            FeatureData f = TranslateLastFeature(InventorSampleParts.ShelledBoxPart());

            Assert.Equal("shell", f.Kind);
            FaceDressData dress = Assert.IsType<FaceDressData>(f.Shell!);
            Assert.Equal(0.5, dress.Value);
            GeomFaceRefData face = Assert.Single(dress.GeomFaces);
            Assert.Equal(new double[] { 2, 1.5, 5 }, face.Centroid);
            Assert.Equal(new double[] { 0, 0, 1 }, face.Normal);
        }

        [Fact]
        public void Hole_emits_diameter_depth_and_a_placement_face_descriptor()
        {
            FeatureData f = TranslateLastFeature(InventorSampleParts.HoledBoxPart());

            Assert.Equal("hole", f.Kind);
            HoleData hole = Assert.IsType<HoleData>(f.Hole!);
            Assert.Equal(1.0, hole.Diameter);
            Assert.Equal(2.0, hole.Depth);
            Assert.Null(hole.ThroughAll); // blind
            Assert.NotNull(hole.GeomFace);
            Assert.Equal(new double[] { 2, 1.5, 5 }, hole.GeomFace!.Centroid);
        }
    }
}
