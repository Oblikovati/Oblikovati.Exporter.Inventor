// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using System.Linq;
using Inventor;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Exercises the REAL DressUpExtractor via faked Inventor fillet/chamfer/shell features, so
    /// edge midpoint/direction and face centroid/normal descriptors are produced with no Inventor.
    /// </summary>
    public sealed class DressUpExtractionTests
    {
        private static InventorDocument Extract(
            IList<FilletFeature>? fillets = null,
            IList<ChamferFeature>? chamfers = null,
            IList<ShellFeature>? shells = null)
        {
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                fillets: fillets, chamfers: chamfers, shells: shells);
            return new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument();
        }

        [Fact]
        public void Fillet_reads_radius_and_an_edge_midpoint_and_direction()
        {
            // A vertical edge from (4,3,0) to (4,3,5).
            var edges = new List<Edge> { new FakeBrepEdge(new double[] { 4, 3, 0 }, new double[] { 4, 3, 5 }) };
            var fillets = new List<FilletFeature> { new FakeFilletFeature("Fillet1", 0.5, edges) };

            var f = Assert.IsType<InventorFillet>(Extract(fillets: fillets).Features.Last());

            Assert.Equal(0.5, f.RadiusCm);
            InventorEdgeDescriptor e = Assert.Single(f.Edges);
            Assert.Equal(new double[] { 4, 3, 2.5 }, e.Midpoint);
            Assert.Equal(new double[] { 0, 0, 1 }, e.Direction);
        }

        [Fact]
        public void Chamfer_reads_distance_and_its_edges()
        {
            var edges = new List<Edge> { new FakeBrepEdge(new double[] { 0, 0, 0 }, new double[] { 0, 0, 5 }) };
            var chamfers = new List<ChamferFeature> { new FakeChamferFeature("Chamfer1", 0.5, edges) };

            var c = Assert.IsType<InventorChamfer>(Extract(chamfers: chamfers).Features.Last());

            Assert.Equal(0.5, c.DistanceCm);
            Assert.Single(c.Edges);
        }

        [Fact]
        public void Shell_reads_thickness_and_a_planar_face_centroid_and_normal()
        {
            // Top face of the box: four corners at z=5, +Z normal.
            var corners = new[]
            {
                new double[] { 0, 0, 5 }, new double[] { 4, 0, 5 }, new double[] { 4, 3, 5 }, new double[] { 0, 3, 5 },
            };
            var faces = new List<Face> { new FakePlanarFace(corners, new double[] { 0, 0, 1 }) };
            var shells = new List<ShellFeature> { new FakeShellFeature("Shell1", 0.5, faces) };

            var s = Assert.IsType<InventorShell>(Extract(shells: shells).Features.Last());

            Assert.Equal(0.5, s.ThicknessCm);
            InventorFaceDescriptor face = Assert.Single(s.RemovedFaces);
            Assert.Equal(new double[] { 2, 1.5, 5 }, face.Centroid); // average of the four corners
            Assert.Equal(new double[] { 0, 0, 1 }, face.Normal);
        }
    }
}
