using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LayerRemapper.Editor.LayerMigration {
    internal enum LayerMigrationOperationType {
        Remap = 0,
        Remove = 1
    }

    [Serializable]
    internal sealed class LayerSnapshotEntry {
        [SerializeField] int index;
        [SerializeField] string name = string.Empty;

        public int Index => index;
        public string Name => name;

        public void Set(int layerIndex, string layerName) {
            index = layerIndex;
            name = layerName ?? string.Empty;
        }
    }

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

    [Serializable]
    internal sealed class LayerRemapEntry {
        [SerializeField] bool enabled = true;
        [SerializeField] LayerMigrationOperationType operationType = LayerMigrationOperationType.Remap;
        [SerializeField] int oldLayerIndex;
        [SerializeField] string oldLayerName = string.Empty;
        [SerializeField] int newLayerIndex;
        [SerializeField] string newLayerName = string.Empty;

        public bool Enabled {
            get => enabled;
            set => enabled = value;
        }

        public int OldLayerIndex {
            get => oldLayerIndex;
            set => oldLayerIndex = Mathf.Clamp(value, 0, 31);
        }

        public LayerMigrationOperationType OperationType {
            get => operationType;
            set => operationType = value;
        }

        public string OldLayerName {
            get => oldLayerName;
            set => oldLayerName = value ?? string.Empty;
        }

        public int NewLayerIndex {
            get => newLayerIndex;
            set => newLayerIndex = Mathf.Clamp(value, 0, 31);
        }

        public string NewLayerName {
            get => newLayerName;
            set => newLayerName = value ?? string.Empty;
        }
    }

    internal sealed class LayerRemapReport {
        readonly List<string> _changedAssets = new();
        readonly List<string> _scanRoots = new();
        readonly List<string> _warnings = new();

        public bool DryRun { get; set; }
        public bool IsValidationOnly { get; set; }
        public bool IsFullProjectScan { get; set; }
        public int AssetsScanned { get; set; }
        public int PrefabsScanned { get; set; }
        public int ScenesScanned { get; set; }
        public int SerializedAssetsScanned { get; set; }
        public int PrefabsChanged { get; set; }
        public int ScenesChanged { get; set; }
        public int SerializedAssetsChanged { get; set; }
        public int GameObjectLayersChanged { get; set; }
        public int LayerMaskPropertiesChanged { get; set; }
        public int ObjectsChanged { get; set; }
        public int MissingScriptsEncountered { get; set; }
        public int RemainingGameObjectOldLayerUsages { get; set; }
        public int RemainingLayerMaskOldLayerUsages { get; set; }

        public IReadOnlyList<string> ChangedAssets => _changedAssets;
        public IReadOnlyList<string> ScanRoots => _scanRoots;
        public IReadOnlyList<string> Warnings => _warnings;

        public void SetScanRoots(IReadOnlyList<string> roots, bool isFullProjectScan) {
            IsFullProjectScan = isFullProjectScan;
            _scanRoots.Clear();
            if (roots == null)
                return;

            for (var i = 0; i < roots.Count; i++) {
                var root = roots[i];
                if (string.IsNullOrWhiteSpace(root))
                    continue;

                if (!_scanRoots.Contains(root))
                    _scanRoots.Add(root);
            }
        }

        public void AddChangedAsset(string path) {
            if (!_changedAssets.Contains(path))
                _changedAssets.Add(path);
        }

        public void AddWarning(string warning) {
            if (string.IsNullOrWhiteSpace(warning))
                return;

            _warnings.Add(warning);
        }

        public string ToDisplayString() {
            var builder = new StringBuilder();
            var modeText = IsValidationOnly ? "Validation" : DryRun ? "Dry Run" : "Apply";
            builder.AppendLine($"Layer Remap Migration Report ({modeText})");
            builder.AppendLine(IsFullProjectScan ? "Scan scope: Full project (Assets/)" : "Scan scope: Filtered roots");
            if (_scanRoots.Count > 0) {
                builder.AppendLine("Scan roots:");
                foreach (var root in _scanRoots)
                    builder.AppendLine($"- {root}");
            }
            builder.AppendLine($"Assets scanned: {AssetsScanned}");
            builder.AppendLine($"Prefabs scanned: {PrefabsScanned}");
            builder.AppendLine($"Scenes scanned: {ScenesScanned}");
            builder.AppendLine($"Serialized assets scanned: {SerializedAssetsScanned}");
            builder.AppendLine($"Prefabs changed: {PrefabsChanged}");
            builder.AppendLine($"Scenes changed: {ScenesChanged}");
            builder.AppendLine($"Serialized assets changed: {SerializedAssetsChanged}");
            builder.AppendLine($"Objects changed: {ObjectsChanged}");
            builder.AppendLine($"GameObject.layer changes: {GameObjectLayersChanged}");
            builder.AppendLine($"LayerMask property changes: {LayerMaskPropertiesChanged}");
            builder.AppendLine($"Missing scripts encountered: {MissingScriptsEncountered}");
            builder.AppendLine($"Remaining source GameObject.layer usages: {RemainingGameObjectOldLayerUsages}");
            builder.AppendLine($"Remaining source LayerMask usages: {RemainingLayerMaskOldLayerUsages}");

            if (_changedAssets.Count > 0) {
                builder.AppendLine("Changed assets/scenes/prefabs:");
                foreach (var asset in _changedAssets)
                    builder.AppendLine($"- {asset}");
            }

            if (_warnings.Count > 0) {
                builder.AppendLine("Warnings:");
                foreach (var warning in _warnings)
                    builder.AppendLine($"- {warning}");
            }

            return builder.ToString();
        }
    }

    internal static class LayerTableUtility {
        const string TagManagerPath = "ProjectSettings/TagManager.asset";

        /// <summary>Captures current layer indices and names from <c>ProjectSettings/TagManager.asset</c>.</summary>
        public static List<LayerSnapshotEntry> CaptureCurrentLayerTable() {
            var result = new List<LayerSnapshotEntry>(32);
            var tagManagerObjects = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (tagManagerObjects.Length == 0)
                return result;

            var serializedTagManager = new SerializedObject(tagManagerObjects[0]);
            var layers = serializedTagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
                return result;

            var count = Mathf.Min(32, layers.arraySize);
            for (var i = 0; i < count; i++) {
                var entryProperty = layers.GetArrayElementAtIndex(i);
                var snapshotEntry = new LayerSnapshotEntry();
                snapshotEntry.Set(i, entryProperty?.stringValue ?? string.Empty);
                result.Add(snapshotEntry);
            }

            return result;
        }

        /// <summary>Looks up the display name for <paramref name="layerIndex"/> within a snapshot/current layer table.</summary>
        /// <returns>The layer name when found; otherwise an empty string.</returns>
        public static string GetLayerName(int layerIndex, IReadOnlyList<LayerSnapshotEntry> table) {
            for (var i = 0; i < table.Count; i++) {
                if (table[i].Index == layerIndex)
                    return table[i].Name;
            }

            return string.Empty;
        }
    }
}
