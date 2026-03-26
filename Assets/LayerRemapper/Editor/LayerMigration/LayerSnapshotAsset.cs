using System.Collections.Generic;
using UnityEngine;

namespace LayerRemapper.Editor.LayerMigration {
    /// <summary>Stores a captured copy of the project's layer table for later migration and validation.</summary>
    public sealed class LayerSnapshotAsset : ScriptableObject {
        [SerializeField] List<LayerSnapshotEntry> entries = new();

        internal IReadOnlyList<LayerSnapshotEntry> Entries => entries;

        internal void SetEntries(IReadOnlyList<LayerSnapshotEntry> source) {
            entries.Clear();
            foreach (var item in source) {
                var clone = new LayerSnapshotEntry();
                clone.Set(item.Index, item.Name);
                entries.Add(clone);
            }
        }
    }
}