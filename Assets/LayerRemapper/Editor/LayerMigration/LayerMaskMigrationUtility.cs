using System.Collections.Generic;
using UnityEditor;

namespace LayerRemapper.Editor.LayerMigration {
    /// <summary>Traverses serialized property trees to remap and validate nested <see cref="UnityEngine.LayerMask"/> values.</summary>
    public static class LayerMaskMigrationUtility {
        /// <summary>Tracks skipped true/semantic Everything masks while migrating and validating serialized properties.</summary>
        public sealed class EverythingMaskSkipCounts {
            /// <summary>Total masks skipped because they were true Everything (<c>-1</c>).</summary>
            public int TrueEverythingMasksSkipped { get; private set; }
            /// <summary>Total masks skipped because they matched the semantic Everything mask.</summary>
            public int SemanticEverythingMasksSkipped { get; private set; }

            /// <summary>Increments the count of skipped true Everything masks.</summary>
            public void IncrementTrueEverythingSkipped() {
                TrueEverythingMasksSkipped++;
            }

            /// <summary>Increments the count of skipped semantic Everything masks.</summary>
            public void IncrementSemanticEverythingSkipped() {
                SemanticEverythingMasksSkipped++;
            }
        }

        /// <summary>Remaps all serialized <see cref="UnityEngine.LayerMask"/> properties in <paramref name="serializedObject"/>.</summary>
        /// <returns>The number of properties whose mask value differs after remapping.</returns>
        public static int RemapLayerMasksInSerializedObject(SerializedObject serializedObject, IReadOnlyDictionary<int, int> remapTable, bool applyChanges) {
            var changedCount = 0;
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.Next(enterChildren)) {
                enterChildren = true;
                if (!IsLayerMaskProperty(iterator))
                    continue;

                var currentMask = iterator.intValue;
                var remappedMask = LayerMaskRemapper.RemapMask(currentMask, remapTable);
                if (currentMask == remappedMask)
                    continue;

                changedCount++;
                if (applyChanges)
                    iterator.intValue = remappedMask;
            }

            if (changedCount > 0 && applyChanges)
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return changedCount;
        }

        /// <summary>Applies remap and remove migration rules to serialized <see cref="UnityEngine.LayerMask"/> properties in <paramref name="serializedObject"/>.</summary>
        /// <returns>The number of properties whose mask value differs after clearing layer bits.</returns>
        public static int MigrateLayerMasksInSerializedObject(SerializedObject serializedObject, IReadOnlyDictionary<int, int> remapTable, HashSet<int> layerIndicesToRemove, bool applyChanges) {
            return MigrateLayerMasksInSerializedObject(
                serializedObject,
                remapTable,
                layerIndicesToRemove,
                applyChanges,
                EverythingMaskRetentionMode.RetainTrueEverythingOnly,
                0
            );
        }

        /// <summary>Applies remap and remove migration rules to serialized <see cref="UnityEngine.LayerMask"/> properties in <paramref name="serializedObject"/>.</summary>
        /// <returns>The number of properties whose mask value differs after clearing layer bits.</returns>
        public static int MigrateLayerMasksInSerializedObject(SerializedObject serializedObject, IReadOnlyDictionary<int, int> remapTable, HashSet<int> layerIndicesToRemove, bool applyChanges, EverythingMaskRetentionMode retentionMode, int semanticEverythingMask, EverythingMaskSkipCounts skipCounts = null) {
            var changedCount = 0;
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.Next(enterChildren)) {
                enterChildren = true;
                if (!IsLayerMaskProperty(iterator))
                    continue;

                var currentMask = iterator.intValue;
                var updatedMask = MigrateMask(currentMask, remapTable, layerIndicesToRemove, retentionMode, semanticEverythingMask, skipCounts);

                if (currentMask == updatedMask)
                    continue;

                changedCount++;
                if (applyChanges)
                    iterator.intValue = updatedMask;
            }

            if (changedCount > 0 && applyChanges)
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return changedCount;
        }

        /// <summary>Applies remap and remove rules to a single <see cref="UnityEngine.LayerMask"/> value.</summary>
        /// <param name="mask">Source mask value to migrate.</param>
        /// <param name="remapTable">Remap rules keyed by source layer index.</param>
        /// <param name="layerIndicesToRemove">Layer indices whose bits should be removed from the output.</param>
        /// <returns>Migrated mask value after remap and remove rules are applied.</returns>
        static int MigrateMask(int mask, IReadOnlyDictionary<int, int> remapTable, HashSet<int> layerIndicesToRemove, EverythingMaskRetentionMode retentionMode, int semanticEverythingMask, EverythingMaskSkipCounts skipCounts) {
            if (ShouldRetainMask(mask, retentionMode, semanticEverythingMask)) {
                if (IsTrueEverythingMask(mask))
                    skipCounts?.IncrementTrueEverythingSkipped();
                else if (IsSemanticEverythingMask(mask, semanticEverythingMask))
                    skipCounts?.IncrementSemanticEverythingSkipped();

                return mask;
            }

            if (remapTable.Count == 0 && layerIndicesToRemove.Count == 0)
                return mask;

            var resultMask = 0;
            for (var layerIndex = 0; layerIndex < 32; layerIndex++) {
                if (!LayerMaskRemapper.ContainsLayerBit(mask, layerIndex))
                    continue;

                if (remapTable.TryGetValue(layerIndex, out var targetLayer)) {
                    resultMask |= 1 << targetLayer;
                    continue;
                }

                if (layerIndicesToRemove.Contains(layerIndex))
                    continue;

                resultMask |= 1 << layerIndex;
            }

            return resultMask;
        }

        /// <summary>Counts serialized <see cref="UnityEngine.LayerMask"/> properties that still contain bits from <paramref name="oldLayerIndices"/>.</summary>
        /// <returns>The number of <see cref="UnityEngine.LayerMask"/> properties that still reference old layers.</returns>
        public static int CountLayerMasksWithOldBits(SerializedObject serializedObject, IEnumerable<int> oldLayerIndices) {
            return CountLayerMasksWithOldBits(
                serializedObject,
                oldLayerIndices,
                EverythingMaskRetentionMode.RetainTrueEverythingOnly,
                0
            );
        }

        /// <summary>Counts serialized <see cref="UnityEngine.LayerMask"/> properties that still contain bits from <paramref name="oldLayerIndices"/>.</summary>
        /// <returns>The number of <see cref="UnityEngine.LayerMask"/> properties that still reference old layers.</returns>
        public static int CountLayerMasksWithOldBits(SerializedObject serializedObject, IEnumerable<int> oldLayerIndices, EverythingMaskRetentionMode retentionMode, int semanticEverythingMask, EverythingMaskSkipCounts skipCounts = null) {
            var count = 0;
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.Next(enterChildren)) {
                enterChildren = true;
                if (!IsLayerMaskProperty(iterator))
                    continue;

                if (ShouldRetainMask(iterator.intValue, retentionMode, semanticEverythingMask)) {
                    if (IsTrueEverythingMask(iterator.intValue))
                        skipCounts?.IncrementTrueEverythingSkipped();
                    else if (IsSemanticEverythingMask(iterator.intValue, semanticEverythingMask))
                        skipCounts?.IncrementSemanticEverythingSkipped();

                    continue;
                }

                if (LayerMaskRemapper.ContainsAnyOldLayerBit(iterator.intValue, oldLayerIndices))
                    count++;
            }

            return count;
        }

        static bool IsLayerMaskProperty(SerializedProperty property) {
            return property.propertyType == SerializedPropertyType.LayerMask;
        }

        /// <summary>Checks whether <paramref name="mask"/> is Unity's true serialized Everything sentinel value.</summary>
        public static bool IsTrueEverythingMask(int mask) {
            return mask == -1;
        }

        /// <summary>Builds a finite bitmask containing every currently defined/selectable Unity layer slot.</summary>
        public static int BuildSemanticEverythingMask() {
            var semanticEverythingMask = 0;
            for (var layerIndex = 0; layerIndex < 32; layerIndex++) {
                if (string.IsNullOrEmpty(UnityEngine.LayerMask.LayerToName(layerIndex)))
                    continue;

                semanticEverythingMask |= 1 << layerIndex;
            }

            return semanticEverythingMask;
        }

        /// <summary>Checks whether <paramref name="mask"/> is equal to the current finite semantic Everything mask.</summary>
        public static bool IsSemanticEverythingMask(int mask, int semanticEverythingMask) {
            if (IsTrueEverythingMask(mask))
                return false;

            return mask == semanticEverythingMask;
        }

        /// <summary>Determines whether <paramref name="mask"/> should be preserved based on Everything retention settings.</summary>
        public static bool ShouldRetainMask(int mask, EverythingMaskRetentionMode retentionMode, int semanticEverythingMask) {
            if (retentionMode == EverythingMaskRetentionMode.NoRetain)
                return false;

            if (IsTrueEverythingMask(mask))
                return true;

            if (retentionMode == EverythingMaskRetentionMode.RetainTrueAndSemanticEverything && IsSemanticEverythingMask(mask, semanticEverythingMask))
                return true;

            return false;
        }
    }
}
