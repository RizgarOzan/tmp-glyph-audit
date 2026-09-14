using System;
using System.Collections.Generic;

namespace RizgarOzan.TmpGlyphAudit
{
    /// <summary>A font that can answer "do you have a glyph for this code point?".</summary>
    public interface IGlyphSource
    {
        string Name { get; }
        bool HasGlyph(uint codePoint);
        IReadOnlyList<IGlyphSource> Fallbacks { get; }
    }

    /// <summary>
    /// Decides whether a text object can draw a code point, searching fonts in the
    /// order TextMeshPro does: the font itself, its fallback list (depth first),
    /// the global fallback list from TMP Settings, then the TMP Settings default font.
    /// Each font is searched at most once per lookup, so fallback cycles are safe.
    /// </summary>
    public sealed class GlyphResolver
    {
        readonly IReadOnlyList<IGlyphSource> _globalFallbacks;
        readonly IGlyphSource _defaultFont;

        public GlyphResolver(IReadOnlyList<IGlyphSource> globalFallbacks = null, IGlyphSource defaultFont = null)
        {
            _globalFallbacks = globalFallbacks ?? new IGlyphSource[0];
            _defaultFont = defaultFont;
        }

        public bool CanRender(IGlyphSource font, uint codePoint)
        {
            var searched = new HashSet<IGlyphSource>();
            if (font != null && Search(font, codePoint, searched)) return true;
            foreach (var fallback in _globalFallbacks)
                if (fallback != null && Search(fallback, codePoint, searched)) return true;
            return _defaultFont != null && Search(_defaultFont, codePoint, searched);
        }

        /// <summary>Code points in <paramref name="text"/> that no font in the chain can draw.</summary>
        public SortedSet<uint> Missing(IGlyphSource font, string text, bool richText = true, bool parseEscapes = true)
        {
            var missing = new SortedSet<uint>();
            foreach (var group in MissingByFont(font, text, richText, parseEscapes)) missing.UnionWith(group.Missing);
            return missing;
        }

        /// <summary>
        /// Code points no font chain can draw, grouped by the font asked for them: the text's
        /// own <paramref name="font"/> or, when <paramref name="fontByName"/> is given, the font
        /// a &lt;font="…"&gt; tag switches to. A tag naming a font <paramref name="fontByName"/>
        /// returns null for is drawn as text, as TMP does.
        /// </summary>
        public List<(IGlyphSource Font, SortedSet<uint> Missing)> MissingByFont(IGlyphSource font, string text,
            bool richText = true, bool parseEscapes = true, Func<string, IGlyphSource> fontByName = null)
        {
            var groups = new List<(IGlyphSource Font, SortedSet<uint> Missing)>();
            var seen = new HashSet<(IGlyphSource, uint)>();
            Func<string, bool> exists = fontByName == null ? null : name => fontByName(name) != null;
            foreach (var (cp, name) in TextScan.Runs(text, richText, parseEscapes, exists))
            {
                var f = name != null && fontByName != null ? fontByName(name) : font;
                if (!seen.Add((f, cp)) || CanRender(f, cp)) continue;
                int g = groups.FindIndex(x => x.Font == f);
                if (g < 0) { g = groups.Count; groups.Add((f, new SortedSet<uint>())); }
                groups[g].Missing.Add(cp);
            }
            return groups;
        }

        static bool Search(IGlyphSource font, uint cp, HashSet<IGlyphSource> searched)
        {
            if (!searched.Add(font)) return false;
            if (font.HasGlyph(cp)) return true;
            var fallbacks = font.Fallbacks;
            if (fallbacks == null) return false;
            foreach (var fallback in fallbacks)
                if (fallback != null && Search(fallback, cp, searched)) return true;
            return false;
        }
    }
}
