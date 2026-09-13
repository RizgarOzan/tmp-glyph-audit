using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RizgarOzan.TmpGlyphAudit
{
    /// <summary>One piece of text that needs characters its font chain cannot draw.</summary>
    public sealed class Finding
    {
        public string Source;      // asset path: scene, prefab or text file
        public string Location;    // hierarchy path or line number inside the source
        public string Font;        // primary font asset the text uses
        public SortedSet<uint> Missing;
    }

    public sealed class AuditReport
    {
        public readonly List<Finding> Findings = new List<Finding>();
        public int TextsScanned;

        public bool Clean => Findings.Count == 0;

        /// <summary>Missing code points per primary font, across all findings.</summary>
        public SortedDictionary<string, SortedSet<uint>> MissingByFont()
        {
            var byFont = new SortedDictionary<string, SortedSet<uint>>();
            foreach (var f in Findings)
            {
                if (!byFont.TryGetValue(f.Font, out var set)) byFont[f.Font] = set = new SortedSet<uint>();
                set.UnionWith(f.Missing);
            }
            return byFont;
        }

        /// <summary>
        /// The missing characters of one font as a plain string - the format the Font
        /// Asset Creator's "Characters from File" / "Custom Characters" option reads.
        /// </summary>
        public string CharacterSet(string font) =>
            MissingByFont().TryGetValue(font, out var set)
                ? string.Concat(set.Select(cp => char.ConvertFromUtf32((int)cp)))
                : "";

        public string ToText()
        {
            var sb = new StringBuilder();
            sb.Append("TMP Glyph Audit: ").Append(TextsScanned).Append(" texts scanned, ");
            if (Clean) return sb.Append("no missing glyphs.").ToString();
            sb.Append(Findings.Count).Append(" with missing glyphs.\n");
            foreach (var pair in MissingByFont())
            {
                sb.Append("\n").Append(pair.Key).Append(" is missing ").Append(pair.Value.Count)
                  .Append(": ").Append(string.Join(", ", pair.Value.Select(TextScan.Describe))).Append('\n');
                foreach (var f in Findings.Where(f => f.Font == pair.Key)
                                          .OrderBy(f => f.Source).ThenBy(f => f.Location))
                    sb.Append("  ").Append(f.Source).Append(" > ").Append(f.Location).Append(": ")
                      .Append(string.Concat(f.Missing.Select(cp => char.ConvertFromUtf32((int)cp)))).Append('\n');
            }
            return sb.ToString();
        }
    }
}
