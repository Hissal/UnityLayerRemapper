# UnityLayerRemapper

UnityLayerRemapper is an editor-focused Unity package for safely migrating layer usage across an existing project.

It helps you remap old layer indices to new ones in bulk, preview all changes before writing anything, apply migration to assets/scenes/prefabs, and validate remaining usages after migration.

## What this package does

- Captures a snapshot of your **old layer table**.
- Lets you define one or more **old index -> new index** remap entries.
- Scans serialized project data under `Assets/`.
- Updates:
  - `GameObject.layer`
  - serialized `LayerMask` fields (including nested objects/collections/serialize reference graphs)
- Supports:
  - **Preview Dry Run** (no writes)
  - **Apply Migration** (writes changes)
  - **Validate Remaining Usages** (reports leftovers)
- Produces a migration report with changed assets and warnings.

## Key remap behavior

Remapping is index-based (names are display-only), and follows these rules:

- `LayerMask` **Everything** (`-1`) is preserved as-is.
- Unmapped bits are preserved.
- Multi-source to single-destination is supported (destination bit is set once).
- Swap remaps are safe (example: `11 <-> 14`): if both source bits are set, both remain set after remap.

## Requirements

- Unity `6000.4` or newer (based on package manifest in `Assets/LayerRemapper/package.json`).
- Editor environment (tooling is in `Editor/` and intended for editor-time migration workflows).

## Installation (Unity Package Manager via Git URL)

This package manifest lives at `Assets/LayerRemapper/package.json`, so install it as a Git dependency with a `path` query.

### Option A: Unity Package Manager UI

1. Open **Window -> Package Manager**.
2. Click **+** (top-left) -> **Add package from git URL...**
3. Enter:

```text
https://github.com/Hissal/UnityLayerRemapper.git?path=/Assets/LayerRemapper
```

Optional pinning examples:

```text
https://github.com/Hissal/UnityLayerRemapper.git?path=/Assets/LayerRemapper#main
https://github.com/Hissal/UnityLayerRemapper.git?path=/Assets/LayerRemapper#v0.1.0
https://github.com/Hissal/UnityLayerRemapper.git?path=/Assets/LayerRemapper#<commit-sha>
```

### Option B: Edit `Packages/manifest.json` manually

Add an entry under `dependencies`:

```json
{
  "dependencies": {
    "com.hissal.unitylayerremapper": "https://github.com/Hissal/UnityLayerRemapper.git?path=/Assets/LayerRemapper"
  }
}
```

## How to use

### 1) Open the migration window

Use:

- **Tools -> Project -> Layer Remap Migration**

### 2) Capture old layers

- Click **Take Current Layer Snapshot** before changing your layer layout.
- Snapshot asset location:
  - `Assets/LayerRemapper/Editor/LayerMigration/LayerMigrationSnapshot.asset`

### 3) Change your project layer table

- Update layers in Unity (TagManager/layer settings) to the new target layout.

### 4) Add remap entries

For each migration rule:

- Set **Old Layer Index** (from snapshot)
- Set **New Layer Index** (current project)
- Keep **Enabled** checked for active entries

### 5) Preview first (recommended)

- Click **Preview Dry Run**
- Review report output:
  - assets scanned
  - prefabs/scenes scanned
  - object/layer/mask change counts
  - remaining old usages
  - warnings/errors

### 6) Apply migration

- Click **Apply Migration**
- Confirm the dialog
- Tool writes updates to affected assets/scenes/prefabs

### 7) Validate leftovers

- Click **Validate Remaining Usages**
- Use report counts to identify remaining old-layer references

## What gets scanned

The migration runner targets project assets under `Assets/` and includes:

- Prefabs (`t:Prefab`)
- Scenes (`t:Scene`)
- Other serialized assets where `SerializedObject` can be traversed

It skips non-target paths/extensions such as `.meta`, `.cs`, `.asmdef`, and already-covered scene/prefab paths.

## Safety and recommended workflow

- Run on a clean branch.
- Commit before applying migration.
- Use dry-run preview before apply.
- After apply, run validation and then your usual project smoke tests.

## Troubleshooting

### "No enabled remap entries found"

- Ensure at least one remap row is enabled.
- Verify old/new indices are set as expected.

### Unexpected leftovers in validation

- Confirm remap table includes all old indices you intend to migrate.
- Check whether old layer usage is intentional in some assets.
- Re-run validation after fixing mapping entries.

### Processing warnings (scene/prefab/asset)

- The tool reports exceptions per path in the report warnings section.
- Resolve the specific asset issue, then rerun preview/apply.

### Missing scripts encountered

- The report tracks missing script components while scanning hierarchies.
- Fix missing scripts separately, then rerun migration for clean verification.

## Development notes

Core implementation locations:

- Remap logic: `Assets/LayerRemapper/LayerMaskRemapper.cs`
- Serialized `LayerMask` traversal: `Assets/LayerRemapper/Editor/LayerMigration/LayerMaskMigrationUtility.cs`
- Migration execution/reporting: `Assets/LayerRemapper/Editor/LayerMigration/LayerRemapMigrationRunner.cs`
- Editor window UI: `Assets/LayerRemapper/Editor/LayerMigration/LayerRemapMigrationWindow.cs`

## Contributing

Contributions and issues are welcome.

Suggested contribution flow:

1. Create a feature/fix branch.
2. Add or update tests for behavior changes.
3. Run local verification in Unity (preview/apply/validate cycle).
4. Open a pull request with a short migration impact summary.

## License

MIT License. See `LICENSE`.
