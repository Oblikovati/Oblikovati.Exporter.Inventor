# Installing the Oblikovati Inventor exporter

This folder is the add-in layout Inventor loads.

```
<this folder>/
  Oblikovati.Exporter.Inventor.addin   manifest Inventor discovers at launch
  bin/                                 the built add-in assembly + dependencies
```

## Install (Inventor 2027)

1. Download and unzip the release.
2. Copy `Oblikovati.Exporter.Inventor.addin` **and** the `bin/` folder into an Inventor
   Addins directory, keeping them side by side. Either:
   - per user: `%APPDATA%\Autodesk\Inventor 2027\Addins\`, or
   - all users: `%PROGRAMDATA%\Autodesk\Inventor 2027\Addins\`.
3. Start Inventor. The add-in loads at startup (`LoadOnStartUp` = 1). Confirm it under
   **Tools ▸ Add-Ins**.

## Use

Open a part (`.ipt`) or assembly (`.iam`), then run **Export to Oblikovati**. The exporter
reads the active document through the Inventor API and writes an `.opd` (part) / `.oad`
(assembly) next to the source file that opens directly in Oblikovati, keeping the
parametric history. (The ribbon command and dialog are wired in a later milestone; the
M0 add-in loads and exposes the export entry point.)
