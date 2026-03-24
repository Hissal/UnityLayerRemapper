Last Updated: 2026-03-24

# LayerRemapper

Project editor tooling for project-wide Unity layer remapping and layer-usage removal migrations.

## Usage

1. Open **Tools → Project → Layer Remap Migration**.
2. Capture a snapshot with **Take Current Layer Snapshot** before changing layers.
3. Configure one or more entries:
   - **Remap**: source layer index -> target layer index
   - **Remove**: source layer index only (GameObjects move to `Default`, `LayerMask` bit is cleared)
4. Run **Preview Dry Run** to review planned changes.
5. Run **Apply Migration** to write updates.
6. Run **Validate Remaining Usages** to find leftovers.

The migration scans serialized inspector data only (prefabs, scenes, ScriptableObjects, and other serialized assets under `Assets/`), updates `GameObject.layer` plus serialized `LayerMask` fields, and preserves unrelated bits.
