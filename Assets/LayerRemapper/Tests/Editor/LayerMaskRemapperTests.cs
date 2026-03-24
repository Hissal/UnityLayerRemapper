using System.Collections.Generic;
using NUnit.Framework;

namespace LayerRemapper.Tests {
    public sealed class LayerMaskRemapperTests {
        [Test]
        public void RemapMask_SingleBit_RemapsToExpectedLayer() {
            var mapping = new Dictionary<int, int> {
                [8] = 12
            };

            var source = 1 << 8;
            var result = LayerMaskRemapper.RemapMask(source, mapping);

            Assert.That(result, Is.EqualTo(1 << 12));
        }

        [Test]
        public void RemapMask_MultipleBits_RemapsAllMappedBits() {
            var mapping = new Dictionary<int, int> {
                [8] = 12,
                [10] = 15
            };

            var source = (1 << 8) | (1 << 10);
            var result = LayerMaskRemapper.RemapMask(source, mapping);

            Assert.That(result, Is.EqualTo((1 << 12) | (1 << 15)));
        }

        [Test]
        public void RemapMask_PreservesUnmappedBits() {
            var mapping = new Dictionary<int, int> {
                [8] = 12
            };

            var source = (1 << 8) | (1 << 17);
            var result = LayerMaskRemapper.RemapMask(source, mapping);

            Assert.That(result, Is.EqualTo((1 << 12) | (1 << 17)));
        }

        [Test]
        public void RemapMask_ManyOldToOneNew_SetsDestinationBitOnce() {
            var mapping = new Dictionary<int, int> {
                [8] = 12,
                [10] = 12
            };

            var source = (1 << 8) | (1 << 10);
            var result = LayerMaskRemapper.RemapMask(source, mapping);

            Assert.That(result, Is.EqualTo(1 << 12));
        }

        [Test]
        public void ContainsAnyOldLayerBit_DetectsLeftoverBits() {
            var source = (1 << 3) | (1 << 18);
            var hasLeftover = LayerMaskRemapper.ContainsAnyOldLayerBit(source, new[] { 2, 3, 4 });

            Assert.That(hasLeftover, Is.True);
        }

        [Test]
        public void ContainsLayerBit_DetectsSetLayerBit() {
            var source = (1 << 6) | (1 << 18);

            Assert.That(LayerMaskRemapper.ContainsLayerBit(source, 18), Is.True);
            Assert.That(LayerMaskRemapper.ContainsLayerBit(source, 7), Is.False);
        }

        [Test]
        public void ClearLayerBit_ClearsOnlySpecifiedBit() {
            var source = (1 << 4) | (1 << 12) | (1 << 17);
            var result = LayerMaskRemapper.ClearLayerBit(source, 12);

            Assert.That(result, Is.EqualTo((1 << 4) | (1 << 17)));
        }

        [Test]
        public void RemapMask_Everything_RemainsUnchanged() {
            var mapping = new Dictionary<int, int> {
                [11] = 14,
                [14] = 11
            };

            var result = LayerMaskRemapper.RemapMask(-1, mapping);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void RemapMask_Swap_WhenBothSourceBitsSet_PreservesBothBits() {
            var mapping = new Dictionary<int, int> {
                [11] = 14,
                [14] = 11
            };

            var source = (1 << 11) | (1 << 14);
            var result = LayerMaskRemapper.RemapMask(source, mapping);

            Assert.That(result, Is.EqualTo(source));
        }
    }
}
