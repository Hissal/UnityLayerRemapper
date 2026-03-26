using System;
using System.Collections.Generic;
using LayerRemapper.Editor.LayerMigration;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LayerRemapper.Tests {
    public sealed class LayerMaskMigrationUtilityTests {
        [Test]
        public void RemapLayerMasksInSerializedObject_RemapsNestedAndCollectionAndSerializeReferenceMasks() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = MaskWith(8);
                holder.nested = new NestedMaskData {
                    mask = MaskWith(9)
                };
                holder.nestedList = new List<NestedMaskData> {
                    new() {
                        mask = MaskWith(10)
                    }
                };
                holder.graph = new GraphMaskNode {
                    mask = MaskWith(11)
                };

                var serializedObject = new SerializedObject(holder);
                var mapping = new Dictionary<int, int> {
                    [8] = 12,
                    [9] = 13,
                    [10] = 14,
                    [11] = 15
                };

                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(serializedObject, mapping, new HashSet<int>(), true);

                Assert.That(changed, Is.EqualTo(4));
                Assert.That(holder.singleMask.value, Is.EqualTo(MaskWith(12).value));
                Assert.That(holder.nested.mask.value, Is.EqualTo(MaskWith(13).value));
                Assert.That(holder.nestedList[0].mask.value, Is.EqualTo(MaskWith(14).value));
                Assert.That(((GraphMaskNode)holder.graph).mask.value, Is.EqualTo(MaskWith(15).value));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void CountLayerMasksWithOldBits_FindsRemainingOldBitsAfterPartialRemap() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = MaskWith(8);
                holder.nested = new NestedMaskData {
                    mask = MaskWith(17)
                };

                var serializedObject = new SerializedObject(holder);
                var leftovers = LayerMaskMigrationUtility.CountLayerMasksWithOldBits(serializedObject, new[] { 8, 9 });

                Assert.That(leftovers, Is.EqualTo(1));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void GameObjectLayerRule_UpdatesLayerViaRemapTable() {
            var gameObject = new GameObject("LayerTestObject");
            try {
                gameObject.layer = 8;
                var remappedLayerMask = LayerMaskRemapper.RemapMask(1 << gameObject.layer, new Dictionary<int, int> { [8] = 12 });
                gameObject.layer = LayerFromMask(remappedLayerMask);

                Assert.That(gameObject.layer, Is.EqualTo(12));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RemapLayerMasksInSerializedObject_EverythingMask_RemainsUnchanged() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = new LayerMask { value = -1 };

                var serializedObject = new SerializedObject(holder);
                var mapping = new Dictionary<int, int> {
                    [11] = 14,
                    [14] = 11
                };

                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(serializedObject, mapping, new HashSet<int>(), true);

                Assert.That(changed, Is.EqualTo(0));
                Assert.That(holder.singleMask.value, Is.EqualTo(-1));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MigrateLayerMasksInSerializedObject_RemoveMode_ClearsRemovedBitAndPreservesOtherBits() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = new LayerMask {
                    value = (1 << 5) | (1 << 12) | (1 << 18)
                };
                holder.nested = new NestedMaskData {
                    mask = new LayerMask {
                        value = (1 << 12) | (1 << 25)
                    }
                };
                holder.nestedList = new List<NestedMaskData> {
                    new() {
                        mask = new LayerMask {
                            value = (1 << 1) | (1 << 12)
                        }
                    }
                };
                holder.graph = new GraphMaskNode {
                    mask = new LayerMask {
                        value = (1 << 12) | (1 << 3)
                    }
                };

                var serializedObject = new SerializedObject(holder);
                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(serializedObject, new Dictionary<int, int>(), new HashSet<int> { 12 }, true);

                Assert.That(changed, Is.EqualTo(4));
                Assert.That(holder.singleMask.value, Is.EqualTo((1 << 5) | (1 << 18)));
                Assert.That(holder.nested.mask.value, Is.EqualTo(1 << 25));
                Assert.That(holder.nestedList[0].mask.value, Is.EqualTo(1 << 1));
                Assert.That(((GraphMaskNode)holder.graph).mask.value, Is.EqualTo(1 << 3));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MigrateLayerMasksInSerializedObject_RemoveMode_EverythingMask_RemainsUnchanged() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = new LayerMask {
                    value = -1
                };

                var serializedObject = new SerializedObject(holder);
                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(
                    serializedObject,
                    new Dictionary<int, int>(),
                    new HashSet<int> { 12 },
                    true
                );

                Assert.That(changed, Is.EqualTo(0));
                Assert.That(holder.singleMask.value, Is.EqualTo(-1));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MigrateLayerMasksInSerializedObject_NoRetain_TrueEverythingMask_IsProcessedNormally() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = new LayerMask {
                    value = -1
                };

                var serializedObject = new SerializedObject(holder);
                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(
                    serializedObject,
                    new Dictionary<int, int>(),
                    new HashSet<int> { 12 },
                    true,
                    EverythingMaskRetentionMode.NoRetain,
                    0
                );

                Assert.That(changed, Is.EqualTo(1));
                Assert.That(holder.singleMask.value, Is.EqualTo(-1 & ~(1 << 12)));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MigrateLayerMasksInSerializedObject_RetainTrueEverythingOnly_TrueEverythingMask_IsPreserved() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = new LayerMask {
                    value = -1
                };

                var serializedObject = new SerializedObject(holder);
                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(
                    serializedObject,
                    new Dictionary<int, int>(),
                    new HashSet<int> { 12 },
                    true,
                    EverythingMaskRetentionMode.RetainTrueEverythingOnly,
                    0
                );

                Assert.That(changed, Is.EqualTo(0));
                Assert.That(holder.singleMask.value, Is.EqualTo(-1));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MigrateLayerMasksInSerializedObject_RetainTrueAndSemanticEverything_TrueEverythingMask_IsPreserved() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = new LayerMask {
                    value = -1
                };

                var serializedObject = new SerializedObject(holder);
                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(
                    serializedObject,
                    new Dictionary<int, int>(),
                    new HashSet<int> { 12 },
                    true,
                    EverythingMaskRetentionMode.RetainTrueAndSemanticEverything,
                    (1 << 3) | (1 << 5)
                );

                Assert.That(changed, Is.EqualTo(0));
                Assert.That(holder.singleMask.value, Is.EqualTo(-1));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MigrateLayerMasksInSerializedObject_NoRetain_SemanticEverythingMask_IsProcessedNormally() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            const int semanticEverythingMask = (1 << 3) | (1 << 5);
            try {
                holder.singleMask = new LayerMask {
                    value = semanticEverythingMask
                };

                var serializedObject = new SerializedObject(holder);
                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(
                    serializedObject,
                    new Dictionary<int, int>(),
                    new HashSet<int> { 5 },
                    true,
                    EverythingMaskRetentionMode.NoRetain,
                    semanticEverythingMask
                );

                Assert.That(changed, Is.EqualTo(1));
                Assert.That(holder.singleMask.value, Is.EqualTo(1 << 3));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MigrateLayerMasksInSerializedObject_RetainTrueEverythingOnly_SemanticEverythingMask_IsProcessedNormally() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            const int semanticEverythingMask = (1 << 3) | (1 << 5);
            try {
                holder.singleMask = new LayerMask {
                    value = semanticEverythingMask
                };

                var serializedObject = new SerializedObject(holder);
                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(
                    serializedObject,
                    new Dictionary<int, int>(),
                    new HashSet<int> { 5 },
                    true,
                    EverythingMaskRetentionMode.RetainTrueEverythingOnly,
                    semanticEverythingMask
                );

                Assert.That(changed, Is.EqualTo(1));
                Assert.That(holder.singleMask.value, Is.EqualTo(1 << 3));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MigrateLayerMasksInSerializedObject_RetainTrueAndSemanticEverything_SemanticEverythingMask_IsPreserved() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            const int semanticEverythingMask = (1 << 3) | (1 << 5);
            try {
                holder.singleMask = new LayerMask {
                    value = semanticEverythingMask
                };

                var serializedObject = new SerializedObject(holder);
                var changed = LayerMaskMigrationUtility.MigrateLayerMasksInSerializedObject(
                    serializedObject,
                    new Dictionary<int, int>(),
                    new HashSet<int> { 5 },
                    true,
                    EverythingMaskRetentionMode.RetainTrueAndSemanticEverything,
                    semanticEverythingMask
                );

                Assert.That(changed, Is.EqualTo(0));
                Assert.That(holder.singleMask.value, Is.EqualTo(semanticEverythingMask));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void CountLayerMasksWithOldBits_EverythingMask_IsNotCountedAsRemainingUsage() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = new LayerMask {
                    value = -1
                };
                holder.nested = new NestedMaskData {
                    mask = MaskWith(12)
                };

                var serializedObject = new SerializedObject(holder);
                var leftovers = LayerMaskMigrationUtility.CountLayerMasksWithOldBits(serializedObject, new[] { 12 });

                Assert.That(leftovers, Is.EqualTo(1));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void CountLayerMasksWithOldBits_NoRetain_TrueEverythingMask_IsCountedWhenContainingSourceLayer() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = new LayerMask {
                    value = -1
                };

                var serializedObject = new SerializedObject(holder);
                var leftovers = LayerMaskMigrationUtility.CountLayerMasksWithOldBits(
                    serializedObject,
                    new[] { 12 },
                    EverythingMaskRetentionMode.NoRetain,
                    0
                );

                Assert.That(leftovers, Is.EqualTo(1));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void CountLayerMasksWithOldBits_RetainTrueEverythingOnly_TrueEverythingMask_IsIgnored() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            try {
                holder.singleMask = new LayerMask {
                    value = -1
                };

                var serializedObject = new SerializedObject(holder);
                var leftovers = LayerMaskMigrationUtility.CountLayerMasksWithOldBits(
                    serializedObject,
                    new[] { 12 },
                    EverythingMaskRetentionMode.RetainTrueEverythingOnly,
                    0
                );

                Assert.That(leftovers, Is.EqualTo(0));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void CountLayerMasksWithOldBits_NoRetain_SemanticEverythingMask_IsCountedWhenContainingSourceLayer() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            const int semanticEverythingMask = (1 << 3) | (1 << 5);
            try {
                holder.singleMask = new LayerMask {
                    value = semanticEverythingMask
                };

                var serializedObject = new SerializedObject(holder);
                var leftovers = LayerMaskMigrationUtility.CountLayerMasksWithOldBits(
                    serializedObject,
                    new[] { 5 },
                    EverythingMaskRetentionMode.NoRetain,
                    semanticEverythingMask
                );

                Assert.That(leftovers, Is.EqualTo(1));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void CountLayerMasksWithOldBits_RetainTrueAndSemanticEverything_SemanticEverythingMask_IsIgnored() {
            var holder = ScriptableObject.CreateInstance<TestMaskHolder>();
            const int semanticEverythingMask = (1 << 3) | (1 << 5);
            try {
                holder.singleMask = new LayerMask {
                    value = semanticEverythingMask
                };

                var serializedObject = new SerializedObject(holder);
                var leftovers = LayerMaskMigrationUtility.CountLayerMasksWithOldBits(
                    serializedObject,
                    new[] { 5 },
                    EverythingMaskRetentionMode.RetainTrueAndSemanticEverything,
                    semanticEverythingMask
                );

                Assert.That(leftovers, Is.EqualTo(0));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void ShouldRetainMask_RespectsRetentionModes() {
            const int semanticEverythingMask = (1 << 3) | (1 << 5);

            Assert.That(
                LayerMaskMigrationUtility.ShouldRetainMask(-1, EverythingMaskRetentionMode.NoRetain, semanticEverythingMask),
                Is.False
            );
            Assert.That(
                LayerMaskMigrationUtility.ShouldRetainMask(-1, EverythingMaskRetentionMode.RetainTrueEverythingOnly, semanticEverythingMask),
                Is.True
            );
            Assert.That(
                LayerMaskMigrationUtility.ShouldRetainMask(semanticEverythingMask, EverythingMaskRetentionMode.RetainTrueEverythingOnly, semanticEverythingMask),
                Is.False
            );
            Assert.That(
                LayerMaskMigrationUtility.ShouldRetainMask(semanticEverythingMask, EverythingMaskRetentionMode.RetainTrueAndSemanticEverything, semanticEverythingMask),
                Is.True
            );
        }

        private static LayerMask MaskWith(int layerIndex) {
            return new LayerMask {
                value = 1 << layerIndex
            };
        }

        private static int LayerFromMask(int mask) {
            for (var i = 0; i < 32; i++) {
                if ((mask & (1 << i)) != 0)
                    return i;
            }

            throw new InvalidOperationException("No layer bit was set.");
        }

        [Serializable]
        private sealed class NestedMaskData {
            public LayerMask mask;
        }

        [Serializable]
        private abstract class GraphNodeBase {
        }

        [Serializable]
        private sealed class GraphMaskNode : GraphNodeBase {
            public LayerMask mask;
        }

        private sealed class TestMaskHolder : ScriptableObject {
            public LayerMask singleMask;
            public NestedMaskData nested;
            public List<NestedMaskData> nestedList = new();
            [SerializeReference] public GraphNodeBase graph;
        }
    }
}
