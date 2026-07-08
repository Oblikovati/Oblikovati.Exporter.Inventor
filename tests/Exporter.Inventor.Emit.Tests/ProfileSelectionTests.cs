// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Text.Json;
using Oblikovati.Exporter.Inventor.Emit;
using Oblikovati.Exporter.Inventor.Fixtures;
using Oblikovati.Exporter.Inventor.Model;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Emit.Tests
{
    public class ProfileSelectionTests
    {
        private const double RectArea = 12.0;             // 4 × 3 cm
        private static readonly double DiskArea = Math.PI * 0.8 * 0.8;
        private static readonly double AnnulusArea = RectArea - DiskArea;

        // The two live profiles the box-with-hole sketch solves to, in an arbitrary order the host
        // does not promise — selection must be robust to it.
        private static IReadOnlyList<LiveProfile> HoleProfiles() => new[]
        {
            new LiveProfile(0, DiskArea, true, 0),
            new LiveProfile(1, AnnulusArea, true, 1),
        };

        [Fact]
        public void Resolves_a_plain_rectangle_region()
        {
            InventorSketch sketch = InventorSampleParts.RectanglePart().Sketches[0];
            RegionKey region = SketchGeometry.RegionForSeed(sketch, new double[] { 2, 1.5 });
            Assert.Equal(RectArea, region.Area, 6);
            Assert.Equal(0, region.Holes);
        }

        [Fact]
        public void Resolves_the_annulus_region_from_an_interior_seed()
        {
            InventorSketch sketch = InventorSampleParts.BoxWithHolePart().Sketches[0];
            RegionKey region = SketchGeometry.RegionForSeed(sketch, new double[] { 0.3, 0.3 });
            Assert.Equal(AnnulusArea, region.Area, 6);
            Assert.Equal(1, region.Holes);
        }

        [Fact]
        public void Resolves_the_inner_disk_region_from_a_central_seed()
        {
            InventorSketch sketch = InventorSampleParts.BoxWithHolePart().Sketches[0];
            RegionKey region = SketchGeometry.RegionForSeed(sketch, new double[] { 2, 1.5 });
            Assert.Equal(DiskArea, region.Area, 6);
            Assert.Equal(0, region.Holes);
        }

        [Fact]
        public void Selects_the_annulus_profile_for_the_annulus_seed()
        {
            InventorSketch sketch = InventorSampleParts.BoxWithHolePart().Sketches[0];
            RegionKey region = SketchGeometry.RegionForSeed(sketch, new double[] { 0.3, 0.3 });
            Assert.Equal(1, ProfileSelector.SelectByRegion(HoleProfiles(), region));
        }

        [Fact]
        public void Selects_the_disk_profile_for_the_central_seed()
        {
            InventorSketch sketch = InventorSampleParts.BoxWithHolePart().Sketches[0];
            RegionKey region = SketchGeometry.RegionForSeed(sketch, new double[] { 2, 1.5 });
            Assert.Equal(0, ProfileSelector.SelectByRegion(HoleProfiles(), region));
        }

        [Fact]
        public void Prefers_the_matching_hole_count_over_a_nearer_area()
        {
            // A disk profile whose area is closer to the target than the true annulus: hole count
            // must still steer selection to the 1-hole profile.
            var profiles = new[]
            {
                new LiveProfile(0, AnnulusArea + 0.01, true, 0),
                new LiveProfile(1, AnnulusArea, true, 1),
            };
            Assert.Equal(1, ProfileSelector.SelectByRegion(profiles, new RegionKey(AnnulusArea, 1)));
        }

        [Fact]
        public void SelectByIndex_returns_a_present_index_and_rejects_a_missing_one()
        {
            IReadOnlyList<LiveProfile> profiles = HoleProfiles();
            Assert.Equal(1, ProfileSelector.SelectByIndex(profiles, 1));
            Assert.Throws<InvalidOperationException>(() => ProfileSelector.SelectByIndex(profiles, 7));
        }

        [Fact]
        public void Parses_the_live_profiles_result()
        {
            using JsonDocument doc = JsonDocument.Parse(
                "{\"profiles\":[{\"index\":0,\"area\":2.01,\"closed\":true,\"holes\":0}," +
                "{\"index\":1,\"area\":9.99,\"closed\":true,\"holes\":1}]}");
            IReadOnlyList<LiveProfile> profiles = ProfileList.Parse(doc.RootElement);

            Assert.Equal(2, profiles.Count);
            Assert.Equal(1, profiles[1].Index);
            Assert.Equal(9.99, profiles[1].Area, 6);
            Assert.True(profiles[1].Closed);
            Assert.Equal(1, profiles[1].Holes);
        }
    }
}
