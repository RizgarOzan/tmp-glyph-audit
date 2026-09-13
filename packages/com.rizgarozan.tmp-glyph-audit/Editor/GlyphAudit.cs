using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RizgarOzan.TmpGlyphAudit.Editor
{
    /// <summary>Finds text that its TMP font chain cannot draw.</summary>
    public sealed class GlyphAudit
    {
        readonly Dictionary<TMP_FontAsset, TmpFontSource> _fonts = new Dictionary<TMP_FontAsset, TmpFontSource>();
        readonly GlyphResolver _resolver;
        readonly TMP_FontAsset _defaultFont;

        public readonly AuditReport Report = new AuditReport();

        public GlyphAudit()
        {
            var (globals, defaultFont) = ReadTmpSettings();
            _defaultFont = defaultFont;
            _resolver = new GlyphResolver(
                globals.Select(f => (IGlyphSource)TmpFontSource.For(f, _fonts)).ToList(),
                TmpFontSource.For(defaultFont, _fonts));
        }

        /// <summary>Runs every scan the settings ask for.</summary>
        public static AuditReport Run(GlyphAuditSettings settings)
        {
            var audit = new GlyphAudit();
            string[] folders = settings.folders.Where(AssetDatabase.IsValidFolder).ToArray();
            if (folders.Length > 0)
            {
                if (settings.scanPrefabs) audit.ScanPrefabs(folders);
                if (settings.scanScenes) audit.ScanScenes(folders);
            }
            foreach (var source in settings.textSources) audit.ScanTextFiles(source);
            return audit.Report;
        }

        /// <summary>Checks one text as the given font would draw it.</summary>
        public void Check(string text, TMP_FontAsset font, bool richText, bool parseEscapes, string source, string location)
        {
            Report.TextsScanned++;
            font = font != null ? font : _defaultFont;
            var missing = _resolver.Missing(TmpFontSource.For(font, _fonts), text, richText, parseEscapes);
            if (missing.Count == 0) return;
            Report.Findings.Add(new Finding
            {
                Source = source,
                Location = location,
                Font = font != null ? TmpFontSource.For(font, _fonts).Name : "(no font)",
                Missing = missing,
            });
        }

        public void ScanObject(GameObject root, string source)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                Check(text.text, text.font, text.richText, text.parseCtrlCharacters, source, HierarchyPath(text.transform));
        }

        public void ScanPrefabs(string[] folders)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) ScanObject(prefab, path);
            }
        }

        /// <summary>
        /// Scenes already open are read as they are in memory; the rest are opened
        /// additively and closed again without saving.
        /// </summary>
        public void ScanScenes(string[] folders)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;   // package scenes are read-only
                var scene = SceneManager.GetSceneByPath(path);
                bool wasOpen = scene.IsValid() && scene.isLoaded;
                if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                foreach (var root in scene.GetRootGameObjects()) ScanObject(root, path);
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        public void ScanTextFiles(GlyphAuditSettings.TextSource source)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(source.fontAsset);
            if (font == null)
            {
                Debug.LogWarning($"TMP Glyph Audit: no TMP font asset at '{source.fontAsset}' - text source '{source.folder}/{source.pattern}' skipped.");
                return;
            }
            if (!Directory.Exists(source.folder)) return;
            foreach (string file in Directory.GetFiles(source.folder, source.pattern, SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                    Check(lines[i], font, source.richText, true, path, "line " + (i + 1));
            }
        }

        /// <summary>
        /// Reads the global fallback list and default font from the project's TMP Settings
        /// asset through SerializedObject: going through TMP_Settings.instance would pop up
        /// the TMP Essential Resources importer in projects that have not imported them.
        /// </summary>
        static (List<TMP_FontAsset>, TMP_FontAsset) ReadTmpSettings()
        {
            var globals = new List<TMP_FontAsset>();
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null) return (globals, null);
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_fallbackFontAssets");
            if (list != null)
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue is TMP_FontAsset f) globals.Add(f);
            var dflt = so.FindProperty("m_defaultFontAsset")?.objectReferenceValue as TMP_FontAsset;
            return (globals, dflt);
        }

        static string HierarchyPath(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
