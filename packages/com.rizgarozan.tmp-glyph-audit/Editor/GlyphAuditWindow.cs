using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RizgarOzan.TmpGlyphAudit.Editor
{
    public sealed class GlyphAuditWindow : EditorWindow
    {
        GlyphAuditSettings _settings;
        AuditReport _report;
        Vector2 _scroll;
        bool _showSettings = true;

        [MenuItem("Window/Text/TMP Glyph Audit")]
        static void Open() => GetWindow<GlyphAuditWindow>("TMP Glyph Audit");

        void OnEnable() => _settings = GlyphAuditSettings.Load();

        void OnGUI()
        {
            _showSettings = EditorGUILayout.Foldout(_showSettings, "What to scan", true);
            if (_showSettings) DrawSettings();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan", GUILayout.Height(28))) Scan();
                using (new EditorGUI.DisabledScope(_report == null))
                    if (GUILayout.Button("Copy report", GUILayout.Height(28), GUILayout.Width(110)))
                        EditorGUIUtility.systemCopyBuffer = _report.ToText();
            }

            if (_report == null) return;
            EditorGUILayout.Space();
            if (_report.Clean)
            {
                EditorGUILayout.HelpBox($"{_report.TextsScanned} texts scanned. Every character can be drawn.", MessageType.Info);
                return;
            }
            EditorGUILayout.HelpBox($"{_report.TextsScanned} texts scanned, {_report.Findings.Count} with missing glyphs.", MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var pair in _report.MissingByFont())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(pair.Key, EditorStyles.boldLabel);
                    if (GUILayout.Button("Export characters…", GUILayout.Width(140))) Export(pair.Key);
                }
                EditorGUILayout.SelectableLabel(string.Join("  ", pair.Value.Select(TextScan.Describe)),
                    EditorStyles.wordWrappedLabel, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2));
                foreach (var f in _report.Findings.Where(f => f.Font == pair.Key).OrderBy(f => f.Source).ThenBy(f => f.Location))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Ping", GUILayout.Width(44)))
                            PingFinding(f);
                        if (GUILayout.Button("Select", GUILayout.Width(52)))
                            SelectFinding(f);
                        string missing = string.Concat(f.Missing.Select(cp => char.ConvertFromUtf32((int)cp)));
                        EditorGUILayout.LabelField($"{f.Source} > {f.Location}", missing);
                    }
                }
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
        }

        void PingFinding(Finding finding) =>
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(finding.Source));

        void SelectFinding(Finding finding)
        {
            if (finding.Source.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(finding.Source);
                if (prefab == null)
                {
                    PingFinding(finding);
                    return;
                }

                var currentStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (currentStage != null && currentStage.assetPath == finding.Source)
                {
                    SelectHierarchyPath(currentStage.prefabContentsRoot, finding.Location);
                    return;
                }

                if (AssetDatabase.OpenAsset(prefab))
                    EditorApplication.delayCall += () => SelectPrefabFinding(finding.Source, finding.Location);
                return;
            }

            if (finding.Source.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                var scene = SceneManager.GetSceneByPath(finding.Source);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    if (!EditorUtility.DisplayDialog(
                            "Open scene to select finding?",
                            $"The scene '{finding.Source}' is not open. Save modified scenes and open it to select '{finding.Location}'?",
                            "Open scene", "Cancel"))
                        return;

                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        return;

                    scene = EditorSceneManager.OpenScene(finding.Source);
                }

                SelectHierarchyPath(scene.GetRootGameObjects(), finding.Location);
                return;
            }

            // Text files and other non-hierarchy findings retain the asset-ping behaviour.
            PingFinding(finding);
        }

        void SelectPrefabFinding(string source, string location)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.assetPath != source)
            {
                Debug.LogWarning($"Could not open prefab stage for '{source}' to select '{location}'.");
                return;
            }

            SelectHierarchyPath(stage.prefabContentsRoot, location);
        }

        static void SelectHierarchyPath(GameObject root, string path)
        {
            if (root == null) return;
            var segments = path.Split('/');
            int index = segments.Length > 0 && segments[0] == root.name ? 1 : 0;
            var current = root.transform;
            for (; index < segments.Length; index++)
            {
                Transform next = null;
                for (int i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    if (child.name != segments[index]) continue;
                    // Siblings may share a name; the first matching child is selected.
                    next = child;
                    break;
                }

                if (next == null)
                {
                    Debug.LogWarning($"Could not find hierarchy path '{path}' under '{root.name}'.");
                    return;
                }

                current = next;
            }

            Selection.activeGameObject = current.gameObject;
            EditorGUIUtility.PingObject(current.gameObject);
        }

        static void SelectHierarchyPath(GameObject[] roots, string path)
        {
            if (roots == null || roots.Length == 0) return;
            var segments = path.Split('/');
            foreach (var root in roots)
            {
                if (segments.Length == 0 || root.name != segments[0]) continue;
                SelectHierarchyPath(root, path);
                return;
            }

            Debug.LogWarning($"Could not find root for hierarchy path '{path}'.");
        }

        void DrawSettings()
        {
            EditorGUI.BeginChangeCheck();
            _settings.scanScenes = EditorGUILayout.Toggle("Scenes", _settings.scanScenes);
            _settings.scanPrefabs = EditorGUILayout.Toggle("Prefabs", _settings.scanPrefabs);
            string folders = EditorGUILayout.TextField(new GUIContent("Folders", "Comma-separated, e.g. Assets/UI, Assets/Scenes"),
                string.Join(", ", _settings.folders));
            _settings.folders = folders.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

            EditorGUILayout.LabelField(new GUIContent("Runtime text files",
                "Text shown only at runtime (localization exports, dialogue). Each line is checked against the chosen font."));
            for (int i = 0; i < _settings.textSources.Count; i++)
            {
                var s = _settings.textSources[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    s.folder = EditorGUILayout.TextField(s.folder);
                    s.pattern = EditorGUILayout.TextField(s.pattern, GUILayout.Width(70));
                    var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(s.fontAsset);
                    font = (TMPro.TMP_FontAsset)EditorGUILayout.ObjectField(font, typeof(TMPro.TMP_FontAsset), false);
                    s.fontAsset = font != null ? AssetDatabase.GetAssetPath(font) : "";
                    s.richText = GUILayout.Toggle(s.richText, "rich", GUILayout.Width(44));
                    if (GUILayout.Button("-", GUILayout.Width(22))) { _settings.textSources.RemoveAt(i); break; }
                }
            }
            if (GUILayout.Button("Add text source", GUILayout.Width(120)))
                _settings.textSources.Add(new GlyphAuditSettings.TextSource());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unity Localization string tables", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Configure a TMP font asset for each locale code. Requires com.unity.localization.", EditorStyles.wordWrappedMiniLabel);
            for (int i = 0; i < _settings.localizationFonts.Count; i++)
            {
                var pair = _settings.localizationFonts[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    pair.localeCode = EditorGUILayout.TextField(pair.localeCode, GUILayout.Width(72));
                    var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(pair.fontAsset);
                    font = (TMPro.TMP_FontAsset)EditorGUILayout.ObjectField(font, typeof(TMPro.TMP_FontAsset), false);
                    pair.fontAsset = font != null ? AssetDatabase.GetAssetPath(font) : "";
                    if (GUILayout.Button("-", GUILayout.Width(22)))
                    {
                        _settings.localizationFonts.RemoveAt(i);
                        break;
                    }
                }
            }
            if (GUILayout.Button("Add locale font", GUILayout.Width(120)))
                _settings.localizationFonts.Add(new GlyphAuditSettings.LocaleFont());
            if (EditorGUI.EndChangeCheck()) _settings.Save();
        }

        void Scan()
        {
            if (_settings.scanScenes && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                return;
            _report = GlyphAudit.Run(_settings);
        }

        void Export(string font)
        {
            string name = Path.GetFileNameWithoutExtension(font) + " missing characters.txt";
            string path = EditorUtility.SaveFilePanel("Characters for the Font Asset Creator", "Assets", name, "txt");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllText(path, _report.CharacterSet(font));
            AssetDatabase.Refresh();
        }
    }
}
