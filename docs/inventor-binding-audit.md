<!-- SPDX-License-Identifier: GPL-2.0-only -->
# Inventor binding audit

The adapter compiles against a hand-written **facade stub** (`stubs/Inventor.Stubs`, assembly
named `Autodesk.Inventor.Interop`) and binds the real interop at load time inside Inventor
(reference-assembly model). With no Inventor install on the build machine, the binding cannot
be compile-verified against the genuine assembly; this audit instead checks each member the
adapter uses against the **Inventor .NET API contract reference** vendored in the workspace at
`Oblikovati.Contracts/Oblikovati.Contracts.CSharp` (the authoritative interface shapes). The
opt-in `binding-check` CI job performs the genuine-assembly compile once Inventor is available.

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

## ⚠️ Modelled compatibly, exact shape noted

| Member | Reference | Note |
|---|---|---|
| `UserParameters[i]` | `UserParameter this[object Index]` (1-based) | stub uses `Parameter this[int]`. The adapter's call (`parameters[i]`, `i` an int, result typed as `Parameter`) is **compatible with both**: int boxes into the `object` index, and `UserParameter` derives from `Parameter`. `UserParameter`-specific members are not needed yet. |

## Not yet exercised

Sketches, features, work features, patterns/mirror, and the assembly occurrence tree are added
in later milestones; their builders will be audited the same way as they land.
