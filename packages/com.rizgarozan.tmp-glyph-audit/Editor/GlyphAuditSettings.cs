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

        /// <summary>Optional Unity Localization string tables, checked with a font per locale code.</summary>
        public List<LocaleFont> localizationFonts = new List<LocaleFont>();

        [Serializable]
        public sealed class LocaleFont
        {
            public string localeCode = "en";
            public string fontAsset = "";
        }

        [Serializable]
        public sealed class TextSource
        {
            public string folder = "Assets";
            public string pattern = "*.txt";
            public string fontAsset = "";   // asset path of a TMP_FontAsset
            public bool richText = true;
        }

        public static GlyphAuditSettings Load()
        {
            return File.Exists(Path)
                ? Load(Path)
                : new GlyphAuditSettings();
        }

        /// <summary>Loads a specific settings file and fails if it does not exist.</summary>
        public static GlyphAuditSettings Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A settings file path is required.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException($"Glyph Audit settings file not found: {path}", path);

            var settings = JsonUtility.FromJson<GlyphAuditSettings>(File.ReadAllText(path));
            if (settings == null)
                throw new InvalidDataException($"Glyph Audit settings file did not contain a settings object: {path}");

            if (settings.localizationFonts == null)
                settings.localizationFonts = new List<LocaleFont>();
            return settings;
        }

        public void Save() => File.WriteAllText(Path, JsonUtility.ToJson(this, true));
    }
}
