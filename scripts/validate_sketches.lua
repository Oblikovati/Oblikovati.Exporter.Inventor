-- SPDX-License-Identifier: GPL-2.0-only
-- Asserts that every restored sketch is fully constrained (DOF 0) and, when it has geometry,
-- yields at least one closed profile. This catches regressions such as a parameter-driven
-- dimension that fails to resolve and collapses the geometry.
-- Run via: oblikovati-cli script run validate_sketches.lua --doc <file>
local sketches = oblikovati.sketch.list().sketches
for _, sk in ipairs(sketches) do
  if sk.dof ~= 0 then
    error(string.format("sketch %d (%s): dof=%d, want 0 (not fully constrained)", sk.index, sk.name, sk.dof))
  end
  if sk.entityCount > 0 then
    local profiles = oblikovati.sketch.profiles{ sketchIndex = sk.index }.profiles
    local closed = 0
    for _, p in ipairs(profiles) do
      if p.closed then closed = closed + 1 end
    end
    if closed < 1 then
      error(string.format("sketch %d (%s): no closed profile (%d entities) — geometry did not close",
        sk.index, sk.name, sk.entityCount))
    end
  end
end
print(string.format("validated %d sketch(es): all DOF 0 with closed profiles", #sketches))
