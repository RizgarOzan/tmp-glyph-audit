using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RizgarOzan.TmpGlyphAudit.Editor
{
    /// <summary>
    /// What to scan. Stored as JSON in ProjectSettings so the window and the
    /// command-line entry point (and CI) read the same configuration.
    /// </summary>
    [Serializable]
    public sealed class GlyphAuditSettings
    {
        public const string Path = "ProjectSettings/TmpGlyphAuditSettings.json";

        /// <summary>Folders whose scenes and prefabs are scanned.</summary>
        public List<string> folders = new List<string> { "Assets" };
        public bool scanScenes = true;
        public bool scanPrefabs = true;

        /// <summary>
        /// Text that is only shown at runtime - localization exports, dialogue files -
        /// checked against the font that will display it.
        /// </summary>
        public List<TextSource> textSources = new List<TextSource>();

        [Serializable]
        public sealed class TextSource
        {
            public string folder = "Assets";
            public string pattern = "*.txt";
            public string fontAsset = "";   // asset path of a TMP_FontAsset
            public bool richText = true;
        }

        public static GlyphAuditSettings Load() =>
            File.Exists(Path) ? JsonUtility.FromJson<GlyphAuditSettings>(File.ReadAllText(Path)) : new GlyphAuditSettings();

        public void Save() => File.WriteAllText(Path, JsonUtility.ToJson(this, true));
    }
}
