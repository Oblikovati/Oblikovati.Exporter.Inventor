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

## Not yet exercised

Sketches, features, work features, patterns/mirror, and the assembly occurrence tree are added
in later milestones; their builders will be audited the same way as they land.
