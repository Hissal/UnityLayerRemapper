using System.Collections.Generic;

namespace LayerRemapper {
    /// <summary>Provides pure bit remap logic for Unity <see cref="UnityEngine.LayerMask"/> values.</summary>
    public static class LayerMaskRemapper {
        /// <summary>Remaps enabled bits in <paramref name="mask"/> from old layer indices to new layer indices.</summary>
        /// <returns>A new mask with remapped bits while preserving unmapped bits.</returns>
        public static int RemapMask(int mask, IReadOnlyDictionary<int, int> remapTable) {
            // Unity uses -1 for LayerMask Everything; preserve this sentinel exactly.
            if (mask == -1)
                return -1;

            var oldBitsToClear = 0;
            var newBitsToSet = 0;

            foreach (var (oldLayer, newLayer) in remapTable) {
                if (oldLayer < 0 || oldLayer > 31 || newLayer < 0 || newLayer > 31)
                    continue;

                var oldBit = 1 << oldLayer;
                if ((mask & oldBit) == 0)
                    continue;

                oldBitsToClear |= oldBit;
                newBitsToSet |= 1 << newLayer;
            }

            return (mask & ~oldBitsToClear) | newBitsToSet;
        }

        /// <summary>Checks whether <paramref name="mask"/> contains the bit for <paramref name="layerIndex"/>.</summary>
        /// <returns><c>true</c> when the bit is present; otherwise, <c>false</c>.</returns>
        public static bool ContainsLayerBit(int mask, int layerIndex) {
            if (layerIndex is < 0 or > 31)
                return false;

            return (mask & (1 << layerIndex)) != 0;
        }

        /// <summary>Clears the bit for <paramref name="layerIndex"/> from <paramref name="mask"/>.</summary>
        /// <returns>The mask with the target bit removed and all other bits preserved.</returns>
        public static int ClearLayerBit(int mask, int layerIndex) {
            if (layerIndex is < 0 or > 31)
                return mask;

            return mask & ~(1 << layerIndex);
        }

        /// <summary>Checks whether <paramref name="mask"/> still contains any bit from <paramref name="oldLayers"/>.</summary>
        /// <returns><c>true</c> when at least one old layer bit is still present; otherwise, <c>false</c>.</returns>
        public static bool ContainsAnyOldLayerBit(int mask, IEnumerable<int> oldLayers) {
            foreach (var layer in oldLayers) {
                if (ContainsLayerBit(mask, layer))
                    return true;
            }

            return false;
        }
    }
}
