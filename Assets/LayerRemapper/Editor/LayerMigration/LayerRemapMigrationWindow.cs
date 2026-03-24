using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LayerRemapper.Editor.LayerMigration {
    /// <summary>Editor tool that previews, applies, and validates project-wide layer remapping in serialized data.</summary>
    public sealed class LayerRemapMigrationWindow : EditorWindow {
        const string SnapshotFolderRoot = "Assets/LayerRemapper/Editor";
        const string SnapshotFolderName = "LayerMigration";
        const string SnapshotDirectory = SnapshotFolderRoot + "/" + SnapshotFolderName;
        const string SnapshotAssetPath = SnapshotDirectory + "/LayerMigrationSnapshot.asset";

        readonly List<LayerRemapEntry> _entries = new();
        [SerializeField] List<string> _rootPaths = new();

        LayerSnapshotAsset _snapshot;
        List<LayerSnapshotEntry> _currentLayers = new();
        Vector2 _scroll;
        string _reportText = string.Empty;

        [MenuItem("Tools/Project/Layer Remap Migration")]
        static void OpenWindow() {
            var window = GetWindow<LayerRemapMigrationWindow>();
            window.titleContent = new GUIContent("Layer Remap Migration");
            window.minSize = new Vector2(780f, 560f);
            window.Show();
        }

        void OnEnable() {
            _currentLayers = LayerTableUtility.CaptureCurrentLayerTable();
            _snapshot = AssetDatabase.LoadAssetAtPath<LayerSnapshotAsset>(SnapshotAssetPath);
        }

        void OnGUI() {
            EditorGUILayout.HelpBox("Run this tool on a clean branch and commit your work before applying migration.", MessageType.Warning);
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawSnapshotSection();
            EditorGUILayout.Space();
            DrawCurrentLayerSection();
            EditorGUILayout.Space();
            DrawRemapEntriesSection();
            EditorGUILayout.Space();
            DrawScanRootsSection();
            EditorGUILayout.Space();
            DrawActionsSection();
            EditorGUILayout.Space();
            DrawReportSection();
            EditorGUILayout.EndScrollView();
        }

        void DrawSnapshotSection() {
            EditorGUILayout.LabelField("1. Old Layer Snapshot", EditorStyles.boldLabel);
            _snapshot = (LayerSnapshotAsset)EditorGUILayout.ObjectField("Snapshot", _snapshot, typeof(LayerSnapshotAsset), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Take Current Layer Snapshot"))
                TakeSnapshot();

            if (GUILayout.Button("Load Existing Snapshot"))
                LoadExistingSnapshot();

            EditorGUILayout.EndHorizontal();

            var snapshotEntries = GetSnapshotEntries();
            if (snapshotEntries.Count > 0) {
                for (var i = 0; i < snapshotEntries.Count; i++) {
                    var entry = snapshotEntries[i];
                    EditorGUILayout.LabelField($"{entry.Index:00}", string.IsNullOrEmpty(entry.Name) ? "<empty>" : entry.Name);
                }
            }
            else {
                EditorGUILayout.HelpBox("No snapshot loaded.", MessageType.Info);
            }
        }

        void DrawCurrentLayerSection() {
            EditorGUILayout.LabelField("2. New Layer State (Current Project)", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh Current Layer Table"))
                _currentLayers = LayerTableUtility.CaptureCurrentLayerTable();

            for (var i = 0; i < _currentLayers.Count; i++) {
                var entry = _currentLayers[i];
                EditorGUILayout.LabelField($"{entry.Index:00}", string.IsNullOrEmpty(entry.Name) ? "<empty>" : entry.Name);
            }
        }

        void DrawRemapEntriesSection() {
            EditorGUILayout.LabelField("3. Migration Entries", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Operations use source layer indices as source of truth. Names are display-only.", MessageType.None);
            if (GUILayout.Button("Add Entry")) {
                _entries.Add(new LayerRemapEntry());
            }

            var snapshotEntries = GetSnapshotEntries();
            for (var i = 0; i < _entries.Count; i++) {
                var entry = _entries[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                entry.Enabled = EditorGUILayout.Toggle("Enabled", entry.Enabled);
                if (GUILayout.Button("Remove", GUILayout.Width(90f))) {
                    _entries.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                entry.OperationType = (LayerMigrationOperationType)EditorGUILayout.EnumPopup("Operation", entry.OperationType);
                entry.OldLayerIndex = EditorGUILayout.IntField("Old Layer Index", entry.OldLayerIndex);
                entry.OldLayerName = LayerTableUtility.GetLayerName(entry.OldLayerIndex, snapshotEntries);
                EditorGUILayout.LabelField("Old Layer Name", string.IsNullOrEmpty(entry.OldLayerName) ? "<unknown/empty>" : entry.OldLayerName);

                if (entry.OperationType == LayerMigrationOperationType.Remap) {
                    entry.NewLayerIndex = EditorGUILayout.IntField("New Layer Index", entry.NewLayerIndex);
                    entry.NewLayerName = LayerTableUtility.GetLayerName(entry.NewLayerIndex, _currentLayers);
                    EditorGUILayout.LabelField("New Layer Name", string.IsNullOrEmpty(entry.NewLayerName) ? "<unknown/empty>" : entry.NewLayerName);
                }
                else {
                    EditorGUILayout.LabelField("Removal Result", "GameObject.layer -> Default (0), LayerMask bit cleared");
                }

                EditorGUILayout.EndVertical();
            }
        }

        void DrawActionsSection() {
            EditorGUILayout.LabelField("5. Preview / Apply / Validation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Dry Run")) {
                var report = LayerRemapMigrationRunner.Preview(_entries, _rootPaths);
                SetReport(report);
            }

            if (GUILayout.Button("Apply Migration")) {
                if (EditorUtility.DisplayDialog(
                        "Apply Layer Remap Migration",
                        "This will modify scenes, prefabs, and serialized assets. Ensure your branch is clean and committed. Continue?",
                        "Apply",
                        "Cancel"
                    )) {
                    var report = LayerRemapMigrationRunner.Apply(_entries, _rootPaths);
                    SetReport(report);
                }
            }

            if (GUILayout.Button("Validate Remaining Usages")) {
                var report = LayerRemapMigrationRunner.Validate(_entries, _rootPaths);
                SetReport(report);
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawScanRootsSection() {
            EditorGUILayout.LabelField("4. Scan Roots", EditorStyles.boldLabel);
            var hasConfiguredRoots = false;
            for (var i = 0; i < _rootPaths.Count; i++) {
                if (!string.IsNullOrWhiteSpace(_rootPaths[i])) {
                    hasConfiguredRoots = true;
                    break;
                }
            }

            EditorGUILayout.HelpBox(
                !hasConfiguredRoots
                    ? "No root paths configured. Full-project scan under Assets/ will be used."
                    : "Scanning is restricted to assets under the configured root paths.",
                MessageType.None
            );
            var scanRootFilter = LayerMigrationScanRootFilter.Create(_rootPaths);
            for (var i = 0; i < scanRootFilter.Warnings.Count; i++)
                EditorGUILayout.HelpBox(scanRootFilter.Warnings[i], MessageType.Warning);

            for (var i = 0; i < _rootPaths.Count; i++) {
                EditorGUILayout.BeginHorizontal();
                _rootPaths[i] = EditorGUILayout.TextField($"Root {i + 1}", _rootPaths[i]);
                if (GUILayout.Button("Remove", GUILayout.Width(90f))) {
                    _rootPaths.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Root Path"))
                _rootPaths.Add("Assets/");

            if (GUILayout.Button("Use Default (Assets/)")) {
                _rootPaths.Clear();
                _rootPaths.Add("Assets/");
            }

            if (GUILayout.Button("Clear Root Paths"))
                _rootPaths.Clear();

            EditorGUILayout.EndHorizontal();
        }

        void DrawReportSection() {
            EditorGUILayout.LabelField("6. Report", EditorStyles.boldLabel);
            if (string.IsNullOrEmpty(_reportText))
                EditorGUILayout.HelpBox("Run preview, apply, or validation to generate a report.", MessageType.Info);
            else
                EditorGUILayout.TextArea(_reportText, GUILayout.MinHeight(180f));
        }

        void SetReport(LayerRemapReport report) {
            _reportText = report.ToDisplayString();
            Debug.Log(_reportText);
            Repaint();
        }

        void TakeSnapshot() {
            _currentLayers = LayerTableUtility.CaptureCurrentLayerTable();
            EnsureSnapshotFolder();

            if (!_snapshot)
                _snapshot = AssetDatabase.LoadAssetAtPath<LayerSnapshotAsset>(SnapshotAssetPath);

            if (!_snapshot) {
                _snapshot = CreateInstance<LayerSnapshotAsset>();
                AssetDatabase.CreateAsset(_snapshot, SnapshotAssetPath);
            }

            _snapshot.SetEntries(_currentLayers);
            EditorUtility.SetDirty(_snapshot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void EnsureSnapshotFolder() {
            if (!AssetDatabase.IsValidFolder("Assets/LayerRemapper"))
                AssetDatabase.CreateFolder("Assets", "LayerRemapper");

            if (!AssetDatabase.IsValidFolder(SnapshotFolderRoot))
                AssetDatabase.CreateFolder("Assets/LayerRemapper", "Editor");

            if (!AssetDatabase.IsValidFolder(SnapshotDirectory))
                AssetDatabase.CreateFolder(SnapshotFolderRoot, SnapshotFolderName);
        }

        void LoadExistingSnapshot() {
            _snapshot = AssetDatabase.LoadAssetAtPath<LayerSnapshotAsset>(SnapshotAssetPath);
            if (!_snapshot)
                EditorUtility.DisplayDialog("Layer Snapshot", "No snapshot asset was found at the default path.", "OK");
        }

        IReadOnlyList<LayerSnapshotEntry> GetSnapshotEntries() {
            if (_snapshot == null)
                return System.Array.Empty<LayerSnapshotEntry>();

            return _snapshot.Entries;
        }
    }
}
