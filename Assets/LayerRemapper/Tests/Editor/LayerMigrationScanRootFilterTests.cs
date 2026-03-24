using LayerRemapper.Editor.LayerMigration;
using NUnit.Framework;

namespace LayerRemapper.Tests {
    public sealed class LayerMigrationScanRootFilterTests {
        [Test]
        public void Create_EmptyRootList_DefaultsToFullAssetsScan() {
            var filter = LayerMigrationScanRootFilter.Create(null);

            Assert.That(filter.IsFullProjectScan, Is.True);
            Assert.That(filter.Roots, Is.EquivalentTo(new[] { "Assets/" }));
            Assert.That(filter.Warnings, Is.Empty);
            Assert.That(filter.Includes("Assets/LayerRemapper/Editor/LayerMigration/LayerRemapMigrationRunner.cs"), Is.True);
        }

        [Test]
        public void Create_NormalizesTrailingSlashAndBackslashes() {
            var filter = LayerMigrationScanRootFilter.Create(new[] { "Assets\\LayerRemapper\\Editor\\LayerMigration" });

            Assert.That(filter.IsFullProjectScan, Is.False);
            Assert.That(filter.Roots, Is.EquivalentTo(new[] { "Assets/LayerRemapper/Editor/LayerMigration/" }));
            Assert.That(filter.Includes("Assets/LayerRemapper/Editor/LayerMigration/LayerRemapMigrationRunner.cs"), Is.True);
            Assert.That(filter.Includes("Assets/LayerRemapper/Tests/Editor/LayerMaskRemapperTests.cs"), Is.False);
        }

        [Test]
        public void Create_DuplicateRoots_AreCollapsed() {
            var filter = LayerMigrationScanRootFilter.Create(new[] { "Assets/LayerRemapper", "Assets/LayerRemapper/" });

            Assert.That(filter.Roots.Count, Is.EqualTo(1));
            Assert.That(filter.Roots[0], Is.EqualTo("Assets/LayerRemapper/"));
            Assert.That(filter.SearchFolders.Count, Is.EqualTo(1));
            Assert.That(filter.SearchFolders[0], Is.EqualTo("Assets/LayerRemapper"));
        }

        [Test]
        public void Create_MultipleRoots_IncludePathsMatchingAnyRoot() {
            var filter = LayerMigrationScanRootFilter.Create(new[] { "Assets/LayerRemapper/Editor/", "Assets/LayerRemapper/Tests/" });

            Assert.That(filter.Includes("Assets/LayerRemapper/Editor/LayerMigration/LayerMigrationModels.cs"), Is.True);
            Assert.That(filter.Includes("Assets/LayerRemapper/Tests/Editor/LayerMaskMigrationUtilityTests.cs"), Is.True);
            Assert.That(filter.Includes("Assets/LayerRemapper/package.json"), Is.False);
        }

        [Test]
        public void Create_InvalidAndNonexistentRoots_ReportWarningsAndAreSkipped() {
            var filter = LayerMigrationScanRootFilter.Create(new[] { "Packages/SomePackage", "Assets/__DefinitelyMissingRoot__/" });

            Assert.That(filter.IsFullProjectScan, Is.False);
            Assert.That(filter.Roots, Is.Empty);
            Assert.That(filter.Warnings.Count, Is.EqualTo(2));
        }
    }
}
