using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RizgarOzan.TmpGlyphAudit.Editor
{
    /// <summary>
    /// A TMP font asset as a glyph source. A character counts as available when it is
    /// already in the asset's character table, or - for Dynamic assets - when the source
    /// font file has a glyph for it, since TMP adds it to the atlas at runtime.
    /// Nothing is added to any atlas: the check never modifies the font asset.
    /// </summary>
    public sealed class TmpFontSource : IGlyphSource
    {
        readonly TMP_FontAsset _font;
        readonly Dictionary<uint, bool> _known = new Dictionary<uint, bool>();
        readonly Dictionary<TMP_FontAsset, TmpFontSource> _cache;
        IReadOnlyList<IGlyphSource> _fallbacks;

        TmpFontSource(TMP_FontAsset font, Dictionary<TMP_FontAsset, TmpFontSource> cache)
        {
            _font = font;
            _cache = cache;
        }

        /// <summary>One source per font asset and cache, so fallback chains share lookups.</summary>
        public static TmpFontSource For(TMP_FontAsset font, Dictionary<TMP_FontAsset, TmpFontSource> cache)
        {
            if (font == null) return null;
            if (!cache.TryGetValue(font, out var source)) cache[font] = source = new TmpFontSource(font, cache);
            return source;
        }

        public TMP_FontAsset Font => _font;

        public string Name
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(_font);
                return string.IsNullOrEmpty(path) ? _font.name : path;
            }
        }

        public IReadOnlyList<IGlyphSource> Fallbacks
        {
            get
            {
                if (_fallbacks != null) return _fallbacks;
                var list = new List<IGlyphSource>();
                if (_font.fallbackFontAssetTable != null)
                    foreach (var fallback in _font.fallbackFontAssetTable)
                        if (fallback != null) list.Add(For(fallback, _cache));
                return _fallbacks = list;
            }
        }

        public bool HasGlyph(uint codePoint)
        {
            if (_known.TryGetValue(codePoint, out bool has)) return has;
            var table = _font.characterLookupTable;
            has = table != null && table.ContainsKey(codePoint);
            if (!has && _font.atlasPopulationMode != AtlasPopulationMode.Static)
                has = FaceHasGlyph(codePoint);
            return _known[codePoint] = has;
        }

        bool FaceHasGlyph(uint codePoint)
        {
            var face = _font.faceInfo;
            FontEngineError loaded = _font.atlasPopulationMode == AtlasPopulationMode.DynamicOS
                ? FontEngine.LoadFontFace(face.familyName, face.styleName)
                : _font.sourceFontFile != null
                    ? FontEngine.LoadFontFace(_font.sourceFontFile)
                    : FontEngineError.Invalid_File;
            return loaded == FontEngineError.Success
                && FontEngine.TryGetGlyphIndex(codePoint, out uint index) && index != 0;
        }
    }
}
