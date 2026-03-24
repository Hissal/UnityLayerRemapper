using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LayerRemapper.Editor.LayerMigration {
    /// <summary>Coordinates scanning and migration/validation across prefabs, scenes, and serialized assets.</summary>
    internal static class LayerRemapMigrationRunner {
        /// <summary>Runs a read-only migration pass and reports what would change.</summary>
        public static LayerRemapReport Preview(IReadOnlyList<LayerRemapEntry> entries) {
            return Execute(entries, false, false);
        }

        /// <summary>Runs migration and writes modified serialized assets, prefabs, and scenes.</summary>
        public static LayerRemapReport Apply(IReadOnlyList<LayerRemapEntry> entries) {
            return Execute(entries, true, false);
        }

        /// <summary>Runs validation-only scanning for remaining old layer usages.</summary>
        public static LayerRemapReport Validate(IReadOnlyList<LayerRemapEntry> entries) {
            return Execute(entries, false, true);
        }

        static LayerRemapReport Execute(IReadOnlyList<LayerRemapEntry> entries, bool applyChanges, bool validationOnly) {
            var report = new LayerRemapReport {
                DryRun = !applyChanges,
                IsValidationOnly = validationOnly
            };

            var migrationPlan = BuildMigrationPlan(entries, report);
            if (migrationPlan.SourceLayers.Count == 0)
                report.AddWarning("No enabled migration entries found.");

            ProcessPrefabs(migrationPlan.RemapTable, migrationPlan.RemoveLayers, migrationPlan.SourceLayers, applyChanges, validationOnly, report);
            ProcessScenes(migrationPlan.RemapTable, migrationPlan.RemoveLayers, migrationPlan.SourceLayers, applyChanges, validationOnly, report);
            ProcessOtherAssets(migrationPlan.RemapTable, migrationPlan.RemoveLayers, migrationPlan.SourceLayers, applyChanges, validationOnly, report);

            if (applyChanges)
                AssetDatabase.SaveAssets();

            return report;
        }

        static LayerMigrationPlan BuildMigrationPlan(IReadOnlyList<LayerRemapEntry> entries, LayerRemapReport report) {
            var operationBySourceLayer = new Dictionary<int, LayerMigrationOperationType>();
            var remapTable = new Dictionary<int, int>();
            var removeLayers = new HashSet<int>();
            for (var i = 0; i < entries.Count; i++) {
                var entry = entries[i];
                if (!entry.Enabled)
                    continue;

                if (operationBySourceLayer.TryGetValue(entry.OldLayerIndex, out var existingOperationType) && existingOperationType != entry.OperationType) {
                    report.AddWarning($"Conflicting operation types for source layer {entry.OldLayerIndex}. Keeping {existingOperationType}, skipping {entry.OperationType}.");
                    continue;
                }

                operationBySourceLayer[entry.OldLayerIndex] = entry.OperationType;

                if (entry.OperationType == LayerMigrationOperationType.Remove) {
                    removeLayers.Add(entry.OldLayerIndex);
                    continue;
                }

                if (remapTable.TryGetValue(entry.OldLayerIndex, out var existingNewLayer) && existingNewLayer != entry.NewLayerIndex) {
                    report.AddWarning($"Conflicting remap for source layer {entry.OldLayerIndex}. Keeping {existingNewLayer}, skipping {entry.NewLayerIndex}.");
                    continue;
                }

                remapTable[entry.OldLayerIndex] = entry.NewLayerIndex;
            }

            foreach (var remapPair in remapTable) {
                if (removeLayers.Contains(remapPair.Value))
                    report.AddWarning($"Remap target layer {remapPair.Value} is configured for removal. Remapped values to this layer will be cleared.");
            }

            return new LayerMigrationPlan(remapTable, removeLayers, new List<int>(operationBySourceLayer.Keys));
        }

        static void ProcessPrefabs(IReadOnlyDictionary<int, int> remapTable, HashSet<int> removeLayers, IEnumerable<int> sourceLayers, bool applyChanges, bool validationOnly, LayerRemapReport report) {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (var guid in prefabGuids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                report.AssetsScanned++;
                report.PrefabsScanned++;

                GameObject prefabRoot = null;
                try {
                    prefabRoot = PrefabUtility.LoadPrefabContents(path);
                    var changed = ProcessGameObjectHierarchy(prefabRoot, remapTable, removeLayers, sourceLayers, applyChanges, validationOnly, report);
                    if (applyChanges && changed)
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);

                    if (changed)
                        report.AddChangedAsset(path);
                }
                catch (Exception exception) {
                    report.AddWarning($"Prefab processing failed: {path}. {exception.Message}");
                }
                finally {
                    if (prefabRoot)
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        static void ProcessScenes(IReadOnlyDictionary<int, int> remapTable, HashSet<int> removeLayers, IEnumerable<int> sourceLayers, bool applyChanges, bool validationOnly, LayerRemapReport report) {
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try {
                var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
                foreach (var guid in sceneGuids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    report.AssetsScanned++;
                    report.ScenesScanned++;

                    try {
                        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        var changed = false;
                        var roots = scene.GetRootGameObjects();
                        for (var i = 0; i < roots.Length; i++) {
                            if (ProcessGameObjectHierarchy(roots[i], remapTable, removeLayers, sourceLayers, applyChanges, validationOnly, report))
                                changed = true;
                        }

                        if (applyChanges && changed)
                            EditorSceneManager.SaveScene(scene);

                        if (changed)
                            report.AddChangedAsset(path);

                        EditorSceneManager.CloseScene(scene, true);
                    }
                    catch (Exception exception) {
                        report.AddWarning($"Scene processing failed: {path}. {exception.Message}");
                    }
                }
            }
            finally {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        static void ProcessOtherAssets(IReadOnlyDictionary<int, int> remapTable, HashSet<int> removeLayers, IEnumerable<int> sourceLayers, bool applyChanges, bool validationOnly, LayerRemapReport report) {
            var allPaths = AssetDatabase.GetAllAssetPaths();
            var scannedPaths = new HashSet<string>();
            foreach (var path in allPaths) {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                    continue;

                if (Path.GetExtension(path) is ".unity" or ".prefab" or ".meta" or ".cs" or ".asmdef")
                    continue;

                if (!scannedPaths.Add(path))
                    continue;

                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (assets == null || assets.Length == 0)
                    continue;

                report.AssetsScanned++;

                var changedInPath = false;
                for (var i = 0; i < assets.Length; i++) {
                    var asset = assets[i];
                    if (!asset || asset is MonoScript || asset is DefaultAsset || asset is GameObject || asset is Component)
                        continue;

                    try {
                        var serializedObject = new SerializedObject(asset);
                        var changedLayerMasks = validationOnly
                            ? 0
                            : LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(serializedObject, remapTable, removeLayers, applyChanges);
                        if (applyChanges && changedLayerMasks > 0)
                            serializedObject.Update();

                        var leftovers = LayerMaskMigrationUtility.CountLayerMasksWithOldBits(serializedObject, sourceLayers);

                        report.LayerMaskPropertiesChanged += changedLayerMasks;
                        report.RemainingLayerMaskOldLayerUsages += leftovers;

                        if (changedLayerMasks > 0) {
                            changedInPath = true;
                            if (applyChanges)
                                EditorUtility.SetDirty(asset);
                        }
                    }
                    catch (Exception exception) {
                        report.AddWarning($"Asset processing failed: {path}. {exception.Message}");
                    }
                }

                if (changedInPath)
                    report.AddChangedAsset(path);
            }
        }

        static bool ProcessGameObjectHierarchy(GameObject root, IReadOnlyDictionary<int, int> remapTable, HashSet<int> removeLayers, IEnumerable<int> sourceLayers, bool applyChanges, bool validationOnly, LayerRemapReport report) {
            var changedAny = false;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++) {
                var gameObject = transforms[i].gameObject;
                var originalLayer = gameObject.layer;
                var newLayer = originalLayer;
                if (remapTable.TryGetValue(originalLayer, out var mappedLayer))
                    newLayer = mappedLayer;
                else if (removeLayers.Contains(originalLayer))
                    newLayer = 0;

                if (originalLayer != newLayer) {
                    report.GameObjectLayersChanged++;
                    changedAny = true;
                    if (applyChanges)
                        gameObject.layer = newLayer;
                }

                var layerForValidation = applyChanges ? gameObject.layer : originalLayer;
                if (LayerMaskRemapper.ContainsAnyOldLayerBit(1 << layerForValidation, sourceLayers))
                    report.RemainingGameObjectOldLayerUsages++;

                var components = gameObject.GetComponents<Component>();
                for (var c = 0; c < components.Length; c++) {
                    var component = components[c];
                    if (!component) {
                        report.MissingScriptsEncountered++;
                        continue;
                    }

                    var serializedObject = new SerializedObject(component);
                    var changedLayerMasks = validationOnly
                        ? 0
                        : LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(serializedObject, remapTable, removeLayers, applyChanges);
                    if (applyChanges && changedLayerMasks > 0)
                        serializedObject.Update();

                    var leftovers = LayerMaskMigrationUtility.CountLayerMasksWithOldBits(serializedObject, sourceLayers);

                    if (changedLayerMasks > 0)
                        changedAny = true;

                    report.LayerMaskPropertiesChanged += changedLayerMasks;
                    report.RemainingLayerMaskOldLayerUsages += leftovers;
                }
            }

            if (changedAny)
                report.ObjectsChanged++;

            return changedAny;
        }

        readonly struct LayerMigrationPlan {
            public readonly Dictionary<int, int> RemapTable;
            public readonly HashSet<int> RemoveLayers;
            public readonly List<int> SourceLayers;

            public LayerMigrationPlan(Dictionary<int, int> remapTable, HashSet<int> removeLayers, List<int> sourceLayers) {
                RemapTable = remapTable;
                RemoveLayers = removeLayers;
                SourceLayers = sourceLayers;
            }
        }
    }
}
