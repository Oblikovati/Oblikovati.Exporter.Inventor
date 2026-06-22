# Oblikovati.Exporter.Inventor

An Autodesk **Inventor 2027** add-in that reads the open part/assembly through the Inventor
API and writes a native, fully-parametric **Oblikovati** document (`.opd` part / `.oad`
assembly). It transcribes Inventor's feature history — parameters with formulas, sketches
with constraints and dimensions, sketch-based features, work features, patterns, and the
assembly tree — so the result stays parametric and recomputes in Oblikovati. It does **not**
dump B-rep/STEP geometry.

## Status

Working alpha. The add-in extracts and translates **parameters, units, sketches (fully
constrained), extrude, datum work planes, revolve, rectangular/circular patterns, mirror, and
assemblies** (`.oad` with the component tree), and publishes an **Export to Oblikovati** ribbon
button on the Part and Assembly Tools tabs. Every Inventor binding is compile-verified against
the genuine Autodesk interop on 2025/2026/2027; outputs are round-trip-validated (volumes and
sketch DOF) against the real Oblikovati reader. Pattern/mirror *live* extraction and dress-ups
are the remaining work.

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
For a real build against the genuine interop, set `UseInventorStubs=false`; the vendored
Inventor 2027 interop (`interop/2027`) is used automatically:

```
dotnet build src/Exporter.Inventor.Entry -c Release -p:UseInventorStubs=false
# or another version:
dotnet build src/Exporter.Inventor.Entry -c Release -p:UseInventorStubs=false \
  -p:InventorSdkDir="$PWD/interop/2025"   # or interop/2026
```

The genuine `Autodesk.Inventor.Interop.dll` for each supported Inventor version is **vendored**
in [`interop/<year>`](interop/README.md). Since Inventor 2025 an add-in must ship the matching
interop in its own folder, and Autodesk
[publishes these binaries](https://www.autodesk.com/support/technical/article/caas/tsarticles/ts/1r0objlzvLJeSDzxNV8b87.html)
for exactly that — so they are committed (not fetched) for deterministic builds. They remain
Autodesk's property under Autodesk's license; see `interop/README.md`.

CI runs the real-assembly compile as the `binding-check` job across a matrix of **Inventor 2025,
2026 and 2027** against the vendored interop, so the genuine API catches any binding mismatch the
stub can't — on every PR.

## Distribution

`.github/workflows/release.yml` runs on every merge to `main`: it packages an installable zip
**per Inventor version** (2025/2026/2027) — each bundling the add-in, its dependencies and the
matching `Autodesk.Inventor.Interop.dll` — and attaches them to a GitHub **prerelease**
(`v0.1.<run>`). Build one by hand with:

```
scripts/package.sh 2027 0.1.0 Oblikovati.Exporter.Inventor-0.1.0-2027.zip
```

See `deploy/README.md` for install steps.

## License

GPL-2.0-only.
