-- SPDX-License-Identifier: GPL-2.0-only
-- Prints the active part's solid volume in cm³ (Oblikovati's database unit). Used by the
-- round-trip to assert that a solid-producing feature (e.g. an extrude) yields the expected
-- volume in the real reader.
-- Run via: oblikovati-cli script run volume.lua --doc <file>
print(oblikovati.model.physicalProperties().volume)
