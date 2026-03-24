using System.Collections.Generic;
using UnityEditor;

namespace LayerRemapper.Editor.LayerMigration {
    /// <summary>Traverses serialized property trees to remap and validate nested <see cref="UnityEngine.LayerMask"/> values.</summary>
    public static class LayerMaskMigrationUtility {
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
            var changedCount = 0;
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.Next(enterChildren)) {
                enterChildren = true;
                if (!IsLayerMaskProperty(iterator))
                    continue;

                var currentMask = iterator.intValue;
                var updatedMask = MigrateMask(currentMask, remapTable, layerIndicesToRemove);

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
        static int MigrateMask(int mask, IReadOnlyDictionary<int, int> remapTable, HashSet<int> layerIndicesToRemove) {
            if (mask == -1)
                return -1;

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
            var count = 0;
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.Next(enterChildren)) {
                enterChildren = true;
                if (!IsLayerMaskProperty(iterator))
                    continue;

                if (iterator.intValue == -1)
                    continue;

                if (LayerMaskRemapper.ContainsAnyOldLayerBit(iterator.intValue, oldLayerIndices))
                    count++;
            }

            return count;
        }

        static bool IsLayerMaskProperty(SerializedProperty property) {
            return property.propertyType == SerializedPropertyType.LayerMask;
        }
    }
}
