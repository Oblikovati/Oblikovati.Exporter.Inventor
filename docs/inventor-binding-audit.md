<!-- SPDX-License-Identifier: GPL-2.0-only -->
# Inventor binding audit

The adapter compiles against a hand-written **facade stub** (`stubs/Inventor.Stubs`, assembly
named `Autodesk.Inventor.Interop`) for the no-install CI gate, and binds the real interop at
load time inside Inventor (reference-assembly model).

**The bindings are now compile-verified against the genuine Autodesk interop.** Autodesk
publishes the Inventor Interop binaries for add-in developers
([TS article](https://www.autodesk.com/support/technical/article/caas/tsarticles/ts/1r0objlzvLJeSDzxNV8b87.html)),
so the `binding-check` CI job fetches them (`scripts/fetch-interop.sh`) and builds the add-in
with `-p:UseInventorStubs=false` against **every supported version** on a stock runner:

| Inventor | Interop | Real compile |
|---|---|---|
| 2025 | v29.3 | ✅ |
| 2026 | v30.2 | ✅ |
| 2027 | v31.0 | ✅ |

The tables below additionally cross-check each member against the **Inventor .NET API contract
reference** vendored at `Oblikovati.Contracts/Oblikovati.Contracts.CSharp`.

## ✅ Verified correct against the reference

| Member | Reference signature | Used by |
|---|---|---|
| `_Document.DocumentType` | `DocumentTypeEnum` (get) | adapter kind dispatch |
| `_Document.DisplayName` | `string` (get/set) | document name |
| `_Document.FullFileName` | `string` (get/set) | output directory |
| `_Document.UnitsOfMeasure` | `UnitsOfMeasure` (get) | units |
| `Application.ActiveDocument` | `_Document` (get) | active document |
| `ApplicationAddInServer` | `Activate(ApplicationAddInSite, bool)`, `Deactivate()`, `ExecuteCommand(int)`, `object Automation {get;}` | add-in shell |
| `ApplicationAddInSite.Application` | `Application` (get) | add-in activation |
| `PartDocument.ComponentDefinition` | `PartComponentDefinition` (get) | parameters |
| `PartComponentDefinition.Parameters` | `Parameters` (get) | parameters |
| `Parameters.UserParameters` | `UserParameters` (get) | user parameters (model params are feature dims, captured later) |
| `UserParameters.Count` | `int` (get) | parameter iteration |
| `Parameter.Name` / `Expression` / `Units` | all `string` (get/set) — the reference confirms "units is always retrieved as a string" | parameter rows |
| `UnitsOfMeasure.LengthUnits` / `AngleUnits` | `UnitsTypeEnum` (get/set) | units |
| `UnitsOfMeasure.GetStringFromType(UnitsTypeEnum)` | `string` — reference confirms e.g. `kMillimeterLengthUnits` ↔ `"mm"` | unit abbreviation |
| `DocumentTypeEnum` values | `kPart=12290`, `kAssembly=12291`, `kDrawing=12292`, `kUnknown=12289`, `kPresentation=12293` | kind dispatch (corrected `kUnknown` from a wrong guess) |

## ❌ Found and fixed (the reference caught real mistakes)

| Area | Was (wrong) | Corrected to (reference) |
|---|---|---|
| `Application.StatusBarText` | modelled as a **method** `void StatusBarText(string)` | a **property** `string StatusBarText { get; set; }`; the adapter now assigns it |
| `UnitsTypeEnum` values | guessed (11150/11154/11194…) | genuine constants (`kMillimeter=11269`, `kCentimeter=11268`, `kMeter=11270`, `kInch=11272`, `kFoot=11273`, `kRadian=11278`, `kDegree=11279`) |
| `ApplicationAddInServer.Automation` | `object?` | `object` (non-nullable); impl returns `null!` |

## ❌❌ Found and fixed by the REAL compile (the idealized C# reference missed these)

The contract reference models COM members as ordinary C# properties, so it could not reveal
these interop quirks — only compiling against the genuine assembly did. All three were caught
and fixed:

| Area | Idealized reference said | Genuine interop (and the fix) |
|---|---|---|
| `UserParameters[i]` | returns `Parameter` | returns `UserParameter`, which is **not** implicitly a `Parameter` (separate COM interfaces). The adapter now reads `UserParameter` directly — which exposes `Name`/`Expression` itself — so no cast is needed. |
| `Parameter`/`UserParameter`.`Units` | property `string Units { get; set; }` | **asymmetric COM accessors** (get → `string`, set → `object`), so it imports as a method, not a property. The adapter calls `get_Units()`. |
| `Path` / `File` in the adapter | n/a | the `Inventor` namespace defines its own `Path` and `File` types, which collide with `System.IO` once `using Inventor;` is in scope. Fixed by aliasing `using IO = System.IO;`. |

The stub was updated to mirror these real shapes (`UserParameters[object]` → `UserParameter`
with `get_Units()`), so the stub-mode build and the real-mode build stay consistent.

## ✅ Sketch read surface (real-compile-verified on 2025/2026/2027)

The M3 sketch extractor compiles clean against all three genuine interops. Members used:

| Member | Reference signature |
|---|---|
| `PartComponentDefinition.Sketches` | `PlanarSketches` (get) |
| `PlanarSketches[i]` / `.Count` | `PlanarSketch this[object Index]` (1-based) / `int` |
| `PlanarSketch.Name` | `string` |
| `PlanarSketch.SketchLines` / `.SketchCircles` | `SketchLines` / `SketchCircles` |
| `PlanarSketch.OriginPointGeometry` | `Point` (sketch origin, model space) |
| `PlanarSketch.AxisEntityGeometry` | `Line` (sketch X axis) |
| `PlanarSketch.PlanarEntityGeometry` | `Plane` (its `Normal` completes the frame) |
| `SketchLines[i]` / `SketchCircles[i]` | `SketchLine this[int]` / `SketchCircle this[int]` (1-based, **int**-indexed unlike `PlanarSketches`) |
| `SketchLine.StartSketchPoint` / `.EndSketchPoint` / `.Construction` / `.Geometry3d` | `SketchPoint` / `SketchPoint` / `bool` / `LineSegment` |
| `SketchCircle.CenterSketchPoint` / `.Radius` / `.Construction` | `SketchPoint` / `double` / `bool` |
| `PlanarSketch.SketchArcs` → `SketchArc.CenterSketchPoint`/`.StartSketchPoint`/`.EndSketchPoint`/`.SweepAngle` | center/start/end + sweep sense (positive = CCW); arc ends join a profile (coincidence inferred) |
| `PlanarSketch.SketchSplines` → `SketchSpline.FitPointCount`/`get_FitPoint(i)`/`.Closed` | a fit-spline's ordered points (`get_FitPoint` is a parameterized COM property → method, 1-based) + closed flag |
| `SketchPoint.Geometry` | `Point2d` (sketch coords, cm) |
| `Point2d.X/.Y`, `Point.X/.Y/.Z` | `double` |
| `Plane.Normal`, `Line.Direction` | `UnitVector` |
| `UnitVector.X/.Y/.Z`, `UnitVector.CrossProduct(UnitVector)` | `double`, `UnitVector` (used for Y = normal × X) |

Note the indexer asymmetry the real compile pinned: `PlanarSketches` is **object**-indexed while
`SketchLines`/`SketchCircles` are **int**-indexed (both 1-based). The plane frame is taken
directly from Inventor (origin/axis/normal), not fitted, and 2D points are read in centimetres —
Inventor's database unit, which is the recipe unit, so coordinates pass through unscaled.

Explicit constraints/dimensions are also read (coincidence stays inferred from meeting endpoints):

| Member | Reference signature |
|---|---|
| `PlanarSketch.GeometricConstraints` / `.DimensionConstraints` | the two collections (int-indexed, 1-based) |
| `HorizontalConstraint`/`VerticalConstraint.Entity` | `SketchEntity` (cast `SketchLine`) |
| `ParallelConstraint`/`PerpendicularConstraint.EntityOne`/`.EntityTwo` | `SketchEntity` (cast `SketchLine`) |
| `TwoPointDistanceDimConstraint.PointOne`/`.PointTwo` / `.Parameter` | `SketchPoint` / `Parameter` |
| `RadiusDimConstraint`/`DiameterDimConstraint.Entity` / `.Parameter` | `SketchEntity` (cast `SketchCircle`) / `Parameter` |
| `Parameter.Expression` | `string` (the dimension's driving expression, e.g. "width") |

Constraints/dimensions are matched by their concrete COM type (pattern matching), and their
referenced entities are mapped to the IR curves/points **by COM identity** (one RCW per COM
object), so a dimension links to the exact curve/endpoint it dimensions. Dimensions carry the
parameter's `Expression`, restoring the parametric intent (e.g. a width dim driven by `width`).

## ✅ Feature + work-plane read surface (real-compile-verified on 2025/2026/2027)

The M4 feature extractor compiles clean against all three genuine interops. Members used:

| Member | Reference signature |
|---|---|
| `PartComponentDefinition.Features` / `.WorkPlanes` | `PartFeatures` / `WorkPlanes` |
| `PartFeatures.ExtrudeFeatures` | `ExtrudeFeatures` |
| `ExtrudeFeatures[i]` / `.Count` | `ExtrudeFeature this[object Index]` (1-based) / `int` |
| `ExtrudeFeature.Name` / `.Operation` / `.Profile` / `.Definition` | `string` / `PartFeatureOperationEnum` / `Profile` / `ExtrudeDefinition` |
| `Profile.Parent` | `Sketch` (downcast to `PlanarSketch` for its `Name`) |
| `ExtrudeDefinition.Extent` / `.ExtentType` | `PartFeatureExtent` / `PartFeatureExtentEnum` |
| `DistanceExtent.Distance` / `.Direction` | `Parameter` / `PartFeatureExtentDirectionEnum` |
| `Parameter._Value` | `double` (evaluated value, cm) |
| `WorkPlanes[i]` / `.Count` | `WorkPlane this[object Index]` (1-based) / `int` |
| `WorkPlane.Name` / `.Plane` | `string` / `Plane` (`RootPoint` + `Normal`) |
| `PartFeatureOperationEnum` | `kJoin=20481`, `kCut=20482`, `kIntersect=20483`, `kNewBody=20485` |
| `PartFeatureExtentDirectionEnum` | `kPositive=20993`, `kNegative=20994`, `kSymmetric=20995` |
| `PartFeatureExtentEnum.kDistanceExtent` | `20737` |

Notes the real compile pinned: `Parameter` (a feature's distance) is a **distinct COM interface
from `UserParameter`** — they don't share an inheritance the C# can rely on, so each is read with
its own members (`Parameter._Value` vs `UserParameter.get_Units()`). `Profile.Parent` is typed
`Sketch` (the base), downcast to `PlanarSketch`. The extrude distance is read evaluated from the
distance `Parameter` (cm); a parameter-driven extent is a later refinement.

## ✅ Revolve read surface (real-compile-verified on 2025/2026/2027)

The M5 revolve extractor compiles clean against all three genuine interops. Members used:

| Member | Reference signature |
|---|---|
| `PartFeatures.RevolveFeatures` | `RevolveFeatures` |
| `RevolveFeatures[i]` / `.Count` | `RevolveFeature this[object Index]` (1-based) / `int` |
| `RevolveFeature.Name` / `.Operation` / `.Profile` | `string` / `PartFeatureOperationEnum` / `Profile` |
| `RevolveFeature._AxisEntity` | `SketchLine` (strongly-typed axis; its 2D endpoints become the centerline) |
| `RevolveFeature.ExtentType` / `.Extent` | `PartFeatureExtentEnum` (`kAngleExtent` / `kFullSweepExtent`) / `PartFeatureExtent` |
| `AngleExtent.Angle` | `Parameter` (`._Value`, radians) |

The revolve axis is read from the strongly-typed `_AxisEntity` (a `SketchLine`) and added to the
profile sketch as a centerline curve, so Oblikovati revolves about the sketch's own centerline.

## ✅ Assembly read surface (real-compile-verified on 2025/2026/2027)

The M6 component extractor compiles clean against all three genuine interops. Members used:

| Member | Reference signature |
|---|---|
| `AssemblyDocument.ComponentDefinition` | `AssemblyComponentDefinition` |
| `AssemblyComponentDefinition.Occurrences` | `ComponentOccurrences` |
| `ComponentOccurrences[i]` / `.Count` | `ComponentOccurrence this[int Index]` (1-based) / `int` |
| `ComponentOccurrence.Name` / `.Definition` / `.Transformation` | `string` / `ComponentDefinition` / `Matrix` |
| `ComponentDefinition.Document` | `object` (cast to `_Document`; the referenced part/assembly) |
| `Matrix.get_Cell(Row, Col)` | `double` (1-based; the COM `Cell` parameterized property) |

The placement is read **cell by cell** via `get_Cell(row, col)` (rotation = cells 1..3 × 1..3,
translation = cells 1..3 × 4) rather than `GetMatrixData`, whose array ordering is undocumented
(row- vs column-major) — `get_Cell` is layout-independent. Components shared by several
occurrences are de-duplicated by full file name so each prototype is exported once.

## ✅ Ribbon/command surface (real-compile-verified on 2025/2026/2027)

The M7 ribbon command compiles clean against all three genuine interops. Members used:

| Member | Reference signature |
|---|---|
| `Application.CommandManager` / `.UserInterfaceManager` | `CommandManager` / `UserInterfaceManager` |
| `CommandManager.ControlDefinitions.AddButtonDefinition(...)` | → `ButtonDefinition` |
| `ButtonDefinition.OnExecute` | event `ButtonDefinitionSink_OnExecuteEventHandler(NameValueMap)` |
| `UserInterfaceManager.Ribbons[name]` → `.RibbonTabs[name]` → `.RibbonPanels.Add(...)` | `Ribbon` / `RibbonTab` / `RibbonPanel` |
| `RibbonPanel.CommandControls.AddButton(buttonDef, useLargeIcon)` | → `CommandControl` |

The `OnExecute` handler is a `void(NameValueMap)` method group, which binds to the event against
both the stub and the real assembly. This is compile-verified only; the button's actual
appearance/behaviour needs a live Inventor session to confirm.

## ✅ Pattern / mirror read surface (real-compile-verified on 2025/2026/2027)

The M5b pattern/mirror extractor compiles clean against all three genuine interops. Members used:

| Member | Reference signature |
|---|---|
| `PartFeatures.RectangularPatternFeatures` / `.CircularPatternFeatures` / `.MirrorFeatures` | the three collections (object-indexed, 1-based) |
| `RectangularPatternFeature.XCount`/`YCount`/`XSpacing`/`YSpacing` | `Parameter` (`._Value`) |
| `RectangularPatternFeature.XDirectionEntity` / `.NaturalXDirection` | `object` / `bool` |
| `CircularPatternFeature.Count`/`Angle` / `.AxisEntity` / `.NaturalAxisDirection` | `Parameter` / `object` / `bool` |
| `MirrorFeature.MirrorPlaneEntity` | `object` |
| `*.ParentFeatures` | `ObjectCollection`; each item cast to `PartFeature` for its `Name` |
| `WorkAxis.Line` (`RootPoint` + `Direction`) | the axis point/direction |
| `WorkPlane.Plane` (`RootPoint` + `Normal`) | the mirror plane |
| `Edge.StartVertex`/`StopVertex` → `Vertex.Point` | a straight direction edge |
| `Face.Geometry` (cast `Plane`) | a planar mirror face |

The `object`-typed direction/axis/plane entities are resolved by type-dispatch (work axis, work
plane, straight edge, planar face); an unresolvable entity or an unknown parent-feature name
skips the pattern rather than guessing. Parent features are mapped to IR feature indices **by
name** (COM RCW identity is unreliable), matching how the sketch/feature resolution already works.

## ✅ Dress-up read surface (real-compile-verified on 2025/2026/2027)

The dress-up extractor (fillet/chamfer/shell/draft/hole) compiles clean against all three genuine
interops. Members used:

| Member | Reference signature |
|---|---|
| `PartFeatures.FaceDraftFeatures` / `.HoleFeatures` | the two collections (object-indexed, 1-based) |
| `FaceDraftFeature._DraftAngle` / `._InputFaces` / `._PullDirection` | `Parameter` / `FaceCollection` / `object` (strongly-typed COM accessors) |
| `HoleFeature.HoleDiameter` / `.Depth` / `.ExtentType` / `.PlacementDefinition` | `Parameter` / `double` / `PartFeatureExtentEnum` (`kThroughAllExtent`) / `HolePlacementDefinition` |
| `PointHolePlacementDefinition.Direction` (cast `Face`) | the planar face the hole drills into |

A draft's pull direction is resolved by the same type-dispatch as patterns (planar face/work
plane normal, or work axis/edge direction); its faces use the shared face-descriptor logic. A
hole's placement face is the `Direction` entity of a `PointHolePlacementDefinition` when it is a
planar `Face`; sketch/linear/concentric hole placements are skipped (reported) for now.

The fillet/chamfer/shell members used:

| Member | Reference signature |
|---|---|
| `PartFeatures.FilletFeatures` / `.ChamferFeatures` / `.ShellFeatures` | the three collections (object-indexed, 1-based) |
| `FilletFeature.FilletDefinition` → `.EdgeSetCount` / `get_EdgeSetItem(i)` | `int` / `FilletRadiusEdgeSet` (cast `FilletConstantRadiusEdgeSet`) |
| `FilletConstantRadiusEdgeSet.Edges` / `.Radius` | `EdgeCollection` / `Parameter` |
| `ChamferFeature.ChamferedEdges` / `.Definition.Distance` | `EdgeCollection` / `Parameter` |
| `ShellFeature.Definition.InputFaces` / `.Thickness` | `FaceCollection` / `Parameter` |
| `EdgeCollection[i]` / `FaceCollection[i]` | `object` (cast `Edge` / `Face`) |
| `Edge.StartVertex`/`StopVertex` → `Vertex.Point` | the straight edge's endpoints (→ midpoint + direction) |
| `Face.Geometry` (cast `Plane` → `Normal`) + `Face.Vertices` → `Vertex.Point` | a planar face's normal + centroid (vertex average) |

Edge descriptors are read from the vertices (straight edges); a planar face's centroid is its
vertices' average and its normal its `Plane` geometry — a non-planar face is skipped. The
`get_EdgeSetItem` parameterized COM property imports as a method, like `get_Cell`/`get_Units`.

## ✅ Sweep / loft read surface (real-compile-verified on 2025/2026/2027)

The sweep + loft extractors compile clean against all three genuine interops. Members used:

| Member | Reference signature |
|---|---|
| `PartFeatures.LoftFeatures` / `.SweepFeatures` | the collections (object-indexed, 1-based) |
| `LoftFeature.Name` / `.Operation` / `.Sections` | `string` / `PartFeatureOperationEnum` / `ObjectCollection` |
| `LoftFeature.Sections[i]` (cast `Profile`) → `.Parent.Name` | each section's sketch (mapped to IR index by name) |
| `SweepFeature.Name` / `.Operation` / `.Profile` / `.Path` | `string` / `PartFeatureOperationEnum` / `Profile` / `Path` |
| `Path.Count` / `Path[i]` | `PathEntity` (1-based) |
| `PathEntity.SketchEntity` (cast `SketchLine`) / `.OpposedToSketchEntity` | `object` / `bool` |
| `SketchLine.Geometry3d` → `LineSegment.StartPoint`/`.EndPoint` | the segment's model-space 3D endpoints |

A loft section's parent sketch resolves to the IR sketch index by name (apex/unknown sections
skip). A sweep's path is read as a polyline of its straight segments' 3D endpoints, oriented by
`OpposedToSketchEntity`; a path containing a non-line entity (arc/spline) is skipped — its
tessellation is a later step. Both are also volume-round-tripped (a ~31.4 cm³ cylinder).

## Not yet exercised (live extraction)

- **sweep paths with arcs/splines**: only straight-segment paths are read; curved path entities
  need the `CurveEvaluator` tessellated to a polyline.
- **hole placement variants**: sketch-, linear-, and concentric-placed holes are skipped; only a
  point placement whose `Direction` is a planar `Face` is read.

Also pending: less-common sketch constraints (tangent/concentric/collinear/equal) + angle
dimensions, and control-point splines (only fit-point splines are read).
