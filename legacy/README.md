# Legacy recipe-YAML exporter (archived fallback)

This folder is a **frozen, self-contained copy** of the original Inventor exporter
as it stood immediately before the migration to driving Oblikovati's live API over
the MCP bridge. It is preserved as a fallback in case the recipe path is needed again.

## What it is

The archived pipeline exports an Inventor part/assembly to an Oblikovati **recipe-YAML**
document (`.opd` / `.oad`) by an open-loop translation:

```
Inventor COM API
  -> Exporter.Inventor.Inv        (adapter: Inventor API -> Inventor-neutral IR)
  -> Exporter.Inventor.Model      (the Inventor-neutral IR)
  -> Exporter.Inventor.Translate  (IR -> Oblikovati recipe POCOs)
  -> Exporter.Inventor.Recipe     (recipe POCOs -> YAML .opd/.oad via YamlDotNet)
  -> Exporter.Inventor.Entry      (the COM ApplicationAddInServer add-in that wires it up)
```

The Oblikovati Go reader then reconstructs the model from that recipe.

## Why it was retired

The recipe path is a blind, open-loop round-trip: a foreign C# encoder guesses region
seeds / feature indices from Inventor's model and hopes Oblikovati's independent DCEL
solver reproduces a compatible ordering. That is the root cause of the region-seed
fragility hit during live testing. The active tree instead drives Oblikovati's **live
API over HTTP** (the MCP bridge at `127.0.0.1:7800/mcp`): it selects regions against the
already-solved sketch, validates volume per feature in a closed loop, and lets Oblikovati
serialize the document with its own writer (lossless by construction).

## Status

- **Excluded from the active solution and from CI.** The root
  `Oblikovati.Exporter.Inventor.slnx` does not reference anything under `legacy/`, and
  `dotnet build -c Release` at the repo root builds only the active solution.
- Independently buildable via the solution here:

  ```
  dotnet build legacy/Oblikovati.Exporter.Inventor.Legacy.slnx -c Release
  ```

  This builds the full add-in chain (Inv -> Model -> Translate -> Recipe -> Entry) against
  the compile-only Inventor stub (`stubs/Inventor.Stubs`), exactly as the original CI
  `core` job did.

- A real (non-stub) build against the genuine Autodesk interop still expects the vendored
  `interop/<year>` binaries from the repo root (`-p:UseInventorStubs=false
  -p:InventorSdkDir=<repo>/interop/<year>`); those are intentionally **not** duplicated
  into `legacy/`.

This copy is a snapshot only. Bug fixes and new work land in the active tree, not here.
