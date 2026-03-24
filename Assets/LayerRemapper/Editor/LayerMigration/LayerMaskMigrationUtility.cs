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
        public static int MigrateLayerMasksInSerializedObject(SerializedObject serializedObject, IReadOnlyDictionary<int, int> remapTable, IReadOnlyCollection<int> layerIndicesToRemove, bool applyChanges) {
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

        static int MigrateMask(int mask, IReadOnlyDictionary<int, int> remapTable, IReadOnlyCollection<int> layerIndicesToRemove) {
            if (layerIndicesToRemove.Count == 0)
                return LayerMaskRemapper.RemapMask(mask, remapTable);

            var migratedMask = 0;
            for (var layerIndex = 0; layerIndex < 32; layerIndex++) {
                if (!LayerMaskRemapper.ContainsLayerBit(mask, layerIndex))
                    continue;

                if (remapTable.TryGetValue(layerIndex, out var targetLayer)) {
                    migratedMask |= 1 << targetLayer;
                    continue;
                }

                if (layerIndicesToRemove.Contains(layerIndex))
                    continue;

                migratedMask |= 1 << layerIndex;
            }

            foreach (var layerIndex in layerIndicesToRemove) {
                migratedMask = LayerMaskRemapper.ClearLayerBit(migratedMask, layerIndex);
            }

            return migratedMask;
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
