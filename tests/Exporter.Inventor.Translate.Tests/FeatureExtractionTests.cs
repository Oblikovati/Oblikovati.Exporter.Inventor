// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Inventor;
using Oblikovati.Exporter.Inventor.Inv;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Tests
{
    /// <summary>
    /// Exercises the REAL <see cref="FeatureExtractor"/> via faked Inventor features (the fakes
    /// subclass the stub), so the extraction logic runs end to end with no Inventor install.
    /// </summary>
    public sealed class FeatureExtractionTests
    {
        private static InventorDocument Extract(
            IList<ExtrudeFeature> extrudes, IList<WorkPlane> workPlanes,
            IList<RevolveFeature>? revolves = null)
        {
            var sketches = new List<PlanarSketch> { FakePlanarSketch.Square() }; // named "Square"
            var doc = new FakePartDocument(
                "plate.ipt", @"C:\work\plate.ipt", new FakeUnitsOfMeasure(),
                new List<UserParameter>(), sketches, extrudes, workPlanes, revolves);
            return new InventorSessionAdapter(new FakeInventorApplication(doc)).ExtractActiveDocument();
        }

        [Fact]
        public void Extracts_a_distance_extrude_bound_to_its_sketch()
        {
            var extrudes = new List<ExtrudeFeature>
            {
                new FakeExtrudeFeature("Extrude1", PartFeatureOperationEnum.kNewBodyOperation, "Square", 5),
            };

            InventorDocument ir = Extract(extrudes, new List<WorkPlane>());

            InventorExtrude extrude = Assert.IsType<InventorExtrude>(Assert.Single(ir.Features));
            Assert.Equal("Extrude1", extrude.Name);
            Assert.Equal(0, extrude.SketchIndex); // resolved by sketch name "Square"
            Assert.Equal(InventorOperation.NewBody, extrude.Operation);
            Assert.Equal(5.0, extrude.Distance);
        }

        [Fact]
        public void Extracts_a_user_work_plane_and_skips_the_default_origin_planes()
        {
            var planes = new List<WorkPlane>
            {
                new FakeWorkPlane("XY Plane", new double[] { 0, 0, 0 }, new double[] { 0, 0, 1 }),
                new FakeWorkPlane("Offset Plane", new double[] { 0, 0, 5 }, new double[] { 0, 0, 1 }),
            };

            InventorDocument ir = Extract(new List<ExtrudeFeature>(), planes);

            InventorWorkPlane plane = Assert.Single(ir.WorkPlanes);
            Assert.Equal("Offset Plane", plane.Name);
            Assert.Equal(new double[] { 0, 0, 5 }, plane.Origin);
        }

        [Fact]
        public void Extracts_a_full_revolve_and_injects_the_axis_as_a_centerline()
        {
            var axis = FakeSketchLine.From(0, 0, 0, 4); // vertical axis line in sketch space
            var revolves = new List<RevolveFeature>
            {
                new FakeRevolveFeature("Revolve1", "Square", axis),
            };

            InventorDocument ir = Extract(new List<ExtrudeFeature>(), new List<WorkPlane>(), revolves);

            InventorRevolve revolve = Assert.IsType<InventorRevolve>(Assert.Single(ir.Features));
            Assert.Equal(0, revolve.SketchIndex);
            Assert.Equal(0, revolve.AngleRadians); // full sweep
            // The axis was added to the profile sketch as a centerline curve.
            Assert.Contains(ir.Sketches[0].Curves, c => c.Centerline);
        }
    }
}
