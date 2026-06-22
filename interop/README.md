# Vendored Inventor interop assemblies

These are the official **Autodesk Inventor Interop** binaries (`Autodesk.Inventor.Interop.dll`),
one per supported Inventor major version:

| Folder | Inventor | Interop version |
|---|---|---|
| `2025/` | Autodesk Inventor 2025 | v29.3 |
| `2026/` | Autodesk Inventor 2026 | v30.2 |
| `2027/` | Autodesk Inventor 2027 | v31.0 |

## Why they are committed

Since Inventor 2025 the product is .NET-based, and an add-in **must ship the matching
`Autodesk.Inventor.Interop.dll` in its own folder** alongside the add-in assembly. Autodesk
publishes these interop binaries specifically for add-in developers to download and
**redistribute with their add-in**:

> Download Autodesk Inventor Interop Binaries —
> <https://www.autodesk.com/support/technical/article/caas/tsarticles/ts/1r0objlzvLJeSDzxNV8b87.html>

Committing them here makes the real-assembly build and the `binding-check` CI job deterministic
(no network fetch) and lets the deployable add-in bundle the correct interop per Inventor version.

## License / ownership

`Autodesk.Inventor.Interop.dll` is © Autodesk and remains subject to Autodesk's license. It is
redistributed here only as the interop layer an Inventor add-in must carry, per the Autodesk
download page above. It is **not** part of this project's GPL-licensed source.

## Refreshing

`scripts/fetch-interop.sh <interop-version> interop/<year>` re-downloads a version from the
Autodesk page above (e.g. `scripts/fetch-interop.sh v31.0 interop/2027`).
