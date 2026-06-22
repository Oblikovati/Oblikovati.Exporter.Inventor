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
            IList<ShellFeature>? shells = null,
            IList<FaceDraftFeature>? drafts = null,
            IList<HoleFeature>? holes = null)
        {
            var doc = new FakePartDocument(
                "p.ipt", @"C:\work\p.ipt", new FakeUnitsOfMeasure(), new List<UserParameter>(),
                fillets: fillets, chamfers: chamfers, shells: shells, drafts: drafts, holes: holes);
            return new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument();
        }

        private static FakePlanarFace TopFace() => new FakePlanarFace(
            new[]
            {
                new double[] { 0, 0, 5 }, new double[] { 4, 0, 5 }, new double[] { 4, 3, 5 }, new double[] { 0, 3, 5 },
            },
            new double[] { 0, 0, 1 });

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

        [Fact]
        public void Draft_reads_angle_faces_and_pull_direction_from_a_planar_face()
        {
            var faces = new List<Face> { TopFace() };
            var drafts = new List<FaceDraftFeature>
            {
                new FakeFaceDraftFeature("Draft1", 0.1, faces, TopFace()), // pull off the top face's normal
            };

            var d = Assert.IsType<InventorDraft>(Extract(drafts: drafts).Features.Last());

            Assert.Equal(0.1, d.AngleRadians);
            Assert.Equal(new double[] { 0, 0, 1 }, d.Pull);
            Assert.Single(d.Faces);
        }

        [Fact]
        public void Hole_reads_diameter_depth_throughall_and_a_point_placement_face()
        {
            var holes = new List<HoleFeature>
            {
                new FakeHoleFeature("Hole1", 1, 2, throughAll: false, placementFace: TopFace()),
            };

            var h = Assert.IsType<InventorHole>(Extract(holes: holes).Features.Last());

            Assert.Equal(1.0, h.DiameterCm);
            Assert.Equal(2.0, h.DepthCm);
            Assert.False(h.ThroughAll);
            Assert.Equal(new double[] { 2, 1.5, 5 }, h.PlacementFace.Centroid);
            Assert.Equal(new double[] { 0, 0, 1 }, h.PlacementFace.Normal);
            Assert.Null(h.Center); // no centre point -> drilled at the face centroid
        }

        [Fact]
        public void Hole_reads_its_explicit_drill_center()
        {
            var holes = new List<HoleFeature>
            {
                new FakeHoleFeature("Hole1", 1, 2, throughAll: false, placementFace: TopFace(),
                    centers: new[] { new double[] { 1, 2, 5 } }),
            };

            var h = Assert.IsType<InventorHole>(Extract(holes: holes).Features.Last());

            Assert.Equal(new double[] { 1, 2, 5 }, h.Center);
        }

        [Fact]
        public void Hole_with_several_center_points_expands_to_one_hole_each()
        {
            var holes = new List<HoleFeature>
            {
                new FakeHoleFeature("Holes", 1, 2, throughAll: true, placementFace: TopFace(),
                    centers: new[] { new double[] { 1, 1, 5 }, new double[] { 3, 1, 5 }, new double[] { 1, 2, 5 } }),
            };

            var ir = Extract(holes: holes);
            var emitted = ir.Features.OfType<InventorHole>().ToList();

            Assert.Equal(3, emitted.Count);
            Assert.All(emitted, e => Assert.True(e.ThroughAll));
            Assert.Equal(new double[] { 3, 1, 5 }, emitted[1].Center);
        }
    }
}
