using System.Collections.Generic;

namespace LayerRemapper {
    /// <summary>Provides pure bit remap logic for Unity <see cref="UnityEngine.LayerMask"/> values.</summary>
    public static class LayerMaskRemapper {
        /// <summary>Remaps enabled bits in <paramref name="mask"/> from old layer indices to new layer indices.</summary>
        /// <returns>A new mask with remapped bits while preserving unmapped bits.</returns>
        public static int RemapMask(int mask, IReadOnlyDictionary<int, int> remapTable) {
            var result = mask;
            foreach (var pair in remapTable) {
                var oldLayer = pair.Key;
                var newLayer = pair.Value;
                if (oldLayer < 0 || oldLayer > 31 || newLayer < 0 || newLayer > 31)
                    continue;

                var oldBit = 1 << oldLayer;
                if ((mask & oldBit) == 0)
                    continue;

                result &= ~oldBit;
                result |= 1 << newLayer;
            }

            return result;
        }

        /// <summary>Checks whether <paramref name="mask"/> still contains any bit from <paramref name="oldLayers"/>.</summary>
        /// <returns><c>true</c> when at least one old layer bit is still present; otherwise, <c>false</c>.</returns>
        public static bool ContainsAnyOldLayerBit(int mask, IEnumerable<int> oldLayers) {
            foreach (var layer in oldLayers) {
                if (layer < 0 || layer > 31)
                    continue;

                if ((mask & (1 << layer)) != 0)
                    return true;
            }

            return false;
        }
    }
}
