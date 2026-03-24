using System.Collections.Generic;
using UnityEditor;

namespace LayerRemapper.Editor.LayerMigration {
    public static class LayerMaskMigrationUtility {
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

        private static bool IsLayerMaskProperty(SerializedProperty property) {
            return property.propertyType == SerializedPropertyType.Integer && property.type == "LayerMask";
        }
    }
}
