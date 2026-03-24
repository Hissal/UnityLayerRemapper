using System;
using System.Collections.Generic;
using UnityEditor;

namespace LayerRemapper.Editor.LayerMigration {
    /// <summary>Normalizes configured scan roots and answers whether asset paths are included by those roots.</summary>
    public sealed class LayerMigrationScanRootFilter {
        readonly List<string> _roots;
        readonly List<string> _searchFolders;
        readonly List<string> _warnings;

        /// <summary>Normalized root paths used for inclusion matching.</summary>
        public IReadOnlyList<string> Roots => _roots;
        /// <summary>Folder paths suitable for <see cref="AssetDatabase.FindAssets(string,string[])"/> search roots.</summary>
        public IReadOnlyList<string> SearchFolders => _searchFolders;
        /// <summary>Validation warnings generated while normalizing configured roots.</summary>
        public IReadOnlyList<string> Warnings => _warnings;
        /// <summary>True when no explicit roots were configured and the filter defaults to scanning all <c>Assets/</c>.</summary>
        public bool IsFullProjectScan { get; }

        LayerMigrationScanRootFilter(List<string> roots, List<string> searchFolders, List<string> warnings, bool isFullProjectScan) {
            _roots = roots;
            _searchFolders = searchFolders;
            _warnings = warnings;
            IsFullProjectScan = isFullProjectScan;
        }

        /// <summary>Builds a filter from user-configured root paths.</summary>
        /// <returns>
        /// A filter that defaults to <c>Assets/</c> when no explicit roots are provided, or a filtered include set when roots are configured.
        /// </returns>
        public static LayerMigrationScanRootFilter Create(IEnumerable<string> configuredRoots) {
            var normalizedRoots = new List<string>();
            var warnings = new List<string>();
            var hadExplicitRootInput = false;

            if (configuredRoots != null) {
                foreach (var configuredRoot in configuredRoots) {
                    if (string.IsNullOrWhiteSpace(configuredRoot))
                        continue;

                    hadExplicitRootInput = true;
                    if (!TryNormalizeRoot(configuredRoot, out var normalizedRoot, out var warning)) {
                        warnings.Add(warning);
                        continue;
                    }

                    var folderPath = normalizedRoot.TrimEnd('/');
                    if (!AssetDatabase.IsValidFolder(folderPath)) {
                        warnings.Add($"Scan root does not exist and will be skipped: {normalizedRoot}");
                        continue;
                    }

                    if (!normalizedRoots.Contains(normalizedRoot))
                        normalizedRoots.Add(normalizedRoot);
                }
            }

            if (!hadExplicitRootInput)
                normalizedRoots.Add("Assets/");

            var searchFolders = new List<string>(normalizedRoots.Count);
            for (var i = 0; i < normalizedRoots.Count; i++) {
                var searchFolder = normalizedRoots[i].TrimEnd('/');
                if (!searchFolders.Contains(searchFolder))
                    searchFolders.Add(searchFolder);
            }

            return new LayerMigrationScanRootFilter(normalizedRoots, searchFolders, warnings, !hadExplicitRootInput);
        }

        /// <summary>Returns true when <paramref name="assetPath"/> is under an included root.</summary>
        public bool Includes(string assetPath) {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            var normalizedPath = NormalizeSlashes(assetPath).Trim();
            if (normalizedPath.Equals("Assets", StringComparison.Ordinal))
                normalizedPath = "Assets/";

            if (!normalizedPath.StartsWith("Assets/", StringComparison.Ordinal))
                return false;

            for (var i = 0; i < _roots.Count; i++) {
                if (normalizedPath.StartsWith(_roots[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static bool TryNormalizeRoot(string rootPath, out string normalizedRoot, out string warning) {
            normalizedRoot = string.Empty;
            warning = string.Empty;

            var trimmed = NormalizeSlashes(rootPath).Trim();
            if (trimmed.Length == 0) {
                warning = "Scan root is empty and will be skipped.";
                return false;
            }

            if (trimmed.Equals("Assets", StringComparison.Ordinal))
                trimmed = "Assets/";

            if (!trimmed.StartsWith("Assets/", StringComparison.Ordinal)) {
                warning = $"Invalid scan root '{rootPath}'. Roots must start with 'Assets/'.";
                return false;
            }

            normalizedRoot = trimmed.TrimEnd('/') + "/";
            return true;
        }

        static string NormalizeSlashes(string value) {
            return value.Replace('\\', '/');
        }
    }
}
