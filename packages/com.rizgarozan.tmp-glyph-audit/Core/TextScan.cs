using System.Collections.Generic;

namespace RizgarOzan.TmpGlyphAudit
{
    /// <summary>
    /// Turns a string into the code points TextMeshPro will ask a font for.
    /// Mirrors the parts of TMP's parser that change which glyphs are needed:
    /// rich-text tags (not drawn), &lt;noparse&gt; (drawn literally), escape
    /// sequences such as \u011F (drawn as the character they name), surrogate
    /// pairs (one code point), and characters TMP never draws with a glyph.
    /// </summary>
    public static class TextScan
    {
        const int MaxTagLength = 128; // TMP's own limit (m_htmlTag buffer)

        static readonly HashSet<string> Tags = new HashSet<string>
        {
            "b", "i", "u", "s", "mark", "sub", "sup", "color", "alpha", "a", "size", "sprite",
            "nobr", "style", "font", "link", "font-weight", "noparse", "pos", "voffset", "space",
            "page", "align", "width", "gradient", "cspace", "mspace", "class", "indent",
            "line-indent", "margin", "margin-left", "margin-right", "line-height", "action",
            "scale", "rotate", "table", "th", "tr", "td", "lowercase", "allcaps", "uppercase",
            "smallcaps", "liga", "frac", "material", "br", "cr", "zwsp", "zwj", "nbsp", "shy",
            "strikethrough", "underline",
        };

        /// <summary>Code points that need a glyph, in order of appearance.</summary>
        public static IEnumerable<uint> CodePoints(string text, bool richText = true, bool parseEscapes = true)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            bool noParse = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (parseEscapes && c == '\\' && i + 1 < text.Length)
                {
                    char n = text[i + 1];
                    if (n == 'n' || n == 'r' || n == 't' || n == 'v') { i++; continue; }
                    if ((n == 'u' && TryHex(text, i + 2, 4, out uint u)) ||
                        (n == 'U' && TryHex(text, i + 2, 8, out u)))
                    {
                        i += n == 'u' ? 5 : 9;
                        if (!IsIgnorable(u)) yield return u;
                        continue;
                    }
                }

                if (richText && c == '<' && TryReadTag(text, i, out int end, out string name, out bool closing))
                {
                    if (name == "noparse") { noParse = !closing; i = end; continue; }
                    if (!noParse) { i = end; continue; }
                }

                uint cp = c;
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    cp = (uint)char.ConvertToUtf32(c, text[i + 1]);
                    i++;
                }
                if (!IsIgnorable(cp)) yield return cp;
            }
        }

        /// <summary>Distinct code points, sorted.</summary>
        public static SortedSet<uint> Distinct(string text, bool richText = true, bool parseEscapes = true)
            => new SortedSet<uint>(CodePoints(text, richText, parseEscapes));

        /// <summary>
        /// Characters TMP handles without looking up a glyph: control characters,
        /// zero-width and bidi marks, line/paragraph separators, the BOM and soft hyphen.
        /// </summary>
        public static bool IsIgnorable(uint cp) =>
            cp < 0x20 || cp == 0x7F || (cp >= 0x80 && cp < 0xA0) ||
            cp == 0xAD ||
            (cp >= 0x200B && cp <= 0x200F) || cp == 0x2028 || cp == 0x2029 ||
            (cp >= 0x2060 && cp <= 0x2064) || cp == 0xFEFF;

        /// <summary>"U+011F 'ğ'"-style label for reports.</summary>
        public static string Describe(uint cp) =>
            $"U+{cp:X4} '{char.ConvertFromUtf32((int)cp)}'";

        static bool TryReadTag(string text, int start, out int end, out string name, out bool closing)
        {
            end = -1; name = null; closing = false;
            int limit = System.Math.Min(text.Length, start + MaxTagLength + 1);
            for (int j = start + 1; j < limit; j++)
            {
                if (text[j] == '<') return false;
                if (text[j] != '>') continue;
                // TMP reads the name from the character right after '<': "< b>" is not a tag.
                string body = text.Substring(start + 1, j - start - 1);
                if (body.Length == 0 || char.IsWhiteSpace(body[0])) return false;
                closing = body[0] == '/';
                if (closing) body = body.Substring(1);
                if (body.Length > 0 && body[0] == '#') { name = "color"; end = j; return true; }
                int stop = 0;
                while (stop < body.Length && body[stop] != '=' && body[stop] != ' ') stop++;
                name = body.Substring(0, stop).ToLowerInvariant();
                if (!Tags.Contains(name)) return false;
                end = j;
                return true;
            }
            return false;
        }

        static bool TryHex(string s, int start, int digits, out uint value)
        {
            value = 0;
            if (start + digits > s.Length) return false;
            for (int k = start; k < start + digits; k++)
            {
                int d = HexDigit(s[k]);
                if (d < 0) return false;
                value = value * 16 + (uint)d;
            }
            return value <= 0x10FFFF && (value < 0xD800 || value > 0xDFFF);
        }

        static int HexDigit(char c) =>
            c >= '0' && c <= '9' ? c - '0' :
            c >= 'a' && c <= 'f' ? c - 'a' + 10 :
            c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;
    }
}
