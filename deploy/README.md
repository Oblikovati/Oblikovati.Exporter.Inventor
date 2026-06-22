# Installing the Oblikovati Inventor exporter

Each release ships one zip **per Inventor version** (2025 / 2026 / 2027). A zip is the add-in
layout Inventor loads, and already bundles the matching `Autodesk.Inventor.Interop.dll`:

```
<unzipped>/
  Oblikovati.Exporter.Inventor.addin   manifest Inventor discovers at launch
  bin/                                 the add-in assembly, its dependencies, and the interop
```

## Install

1. Download the zip matching your Inventor version and unzip it.
2. Copy `Oblikovati.Exporter.Inventor.addin` **and** the `bin/` folder, side by side, into an
   Inventor Addins directory. Either:
   - per user: `%APPDATA%\Autodesk\Inventor <year>\Addins\`, or
   - all users: `%PROGRAMDATA%\Autodesk\Inventor <year>\Addins\`.
3. Start Inventor. The add-in loads at startup; confirm it under **Tools ▸ Add-Ins**.

## Use

Open a part (`.ipt`) or assembly (`.iam`), then on the **Tools** tab click
**Oblikovati ▸ Export to Oblikovati**. The exporter reads the active document through the
Inventor API and writes an `.opd` (part) / `.oad` (assembly) — plus an assembly's component
files — next to the source file, keeping the parametric history. Open the result directly in
Oblikovati. A summary appears on Inventor's status bar.
