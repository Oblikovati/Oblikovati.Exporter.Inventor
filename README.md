# Oblikovati.Exporter.Inventor

An Autodesk **Inventor 2027** add-in that reads the open part/assembly through the Inventor
API and writes a native, fully-parametric **Oblikovati** document (`.opd` part / `.oad`
assembly). It transcribes Inventor's feature history — parameters with formulas, sketches
with constraints and dimensions, sketch-based features, work features, patterns, and the
assembly tree — so the result stays parametric and recomputes in Oblikovati. It does **not**
dump B-rep/STEP geometry.

## Status

**M0 scaffold.** The layered solution, the Inventor stub facade, the add-in shell, the
translation core, and CI are in place and green. The live Inventor extraction (parameters,
sketches, features, …) lands milestone by milestone on this foundation — see the milestone
plan, which mirrors the proven `Oblikovati.Exporter.NX` exporter.

## Architecture

The dependency flows downward. The Inventor-aware projects are `Exporter.Inventor.Inv` (the
adapter) and `Exporter.Inventor.Entry` (the add-in shell, which must implement the Inventor
COM interface); everything below the IR is host-free.

```
Inventor live session
   │   Exporter.Inventor.Inv        Inventor adapter (links real interop, or a stub in CI)
   ▼
Exporter.Inventor.Model            Inventor-neutral IR (plain POCOs, zero Inventor refs)
   │   Exporter.Inventor.Translate  translation core (feature-mapping registry — PURE)
   ▼
Exporter.Inventor.Recipe           Oblikovati recipe POCOs + YAML emitter (YamlDotNet)
   ▼
.opd / .oad
```

The pivot is the **Inventor-neutral IR**: the adapter's only job is `Inventor → IR`, and the
translator (`IR → recipe`) has no Inventor dependency, so it runs and is unit-tested on any
runner. `Exporter.Inventor.Entry` is the COM `ApplicationAddInServer` Inventor loads.

## Build & test (no Inventor required)

```
dotnet build -c Release
dotnet test  -c Release
```

The Inventor-aware projects link a compile-only `Autodesk.Inventor.Interop` stub by default.
For a real build, point `InventorSdkDir` at a folder containing the genuine
`Autodesk.Inventor.Interop.dll`. Autodesk
[publishes the interop binaries](https://www.autodesk.com/support/technical/article/caas/tsarticles/ts/1r0objlzvLJeSDzxNV8b87.html)
for add-in developers, so you don't need a local Inventor install:

```
scripts/fetch-interop.sh v31.0 /tmp/inventor-2027     # 2027; v30.2 = 2026, v29.3 = 2025
dotnet build src/Exporter.Inventor.Entry -c Release \
  -p:UseInventorStubs=false \
  -p:InventorSdkDir=/tmp/inventor-2027
```

(On a machine with Inventor installed, `InventorSdkDir` can instead point at
`…\Autodesk\Inventor 2027\Bin`.) The interop is Autodesk's IP — it is referenced with
`Private=false` (Inventor supplies it at runtime) and is **never** committed to this repo.

CI runs this real-assembly compile as the `binding-check` job across a matrix of **Inventor
2025, 2026 and 2027**, so the genuine interop catches any binding mismatch the stub can't —
on every PR, on a stock runner.

## Distribution

Releases ship the `deploy/` add-in layout (a `.addin` manifest + the built assemblies). See
`deploy/README.md` for install steps.

## License

GPL-2.0-only.
