using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
                            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(f.Source));
                        string missing = string.Concat(f.Missing.Select(cp => char.ConvertFromUtf32((int)cp)));
                        EditorGUILayout.LabelField($"{f.Source} > {f.Location}", missing);
                    }
                }
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
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
            if (EditorGUI.EndChangeCheck()) _settings.Save();
        }

        void Scan()
        {
            if (_settings.scanScenes && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
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
