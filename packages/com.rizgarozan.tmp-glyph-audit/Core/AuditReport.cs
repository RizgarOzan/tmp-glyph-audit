using System;
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
        public string Font;        // font asked for the characters: the text's own, or one a <font> tag switched to
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


        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"textsScanned\": ").Append(TextsScanned)
              .Append(",\n  \"clean\": ").Append(Clean ? "true" : "false")
              .Append(",\n  \"fonts\": [");

            var fonts = MissingByFont().ToArray();
            for (int i = 0; i < fonts.Length; i++)
            {
                var pair = fonts[i];
                sb.Append(i == 0 ? "\n" : ",\n")
                  .Append("    {\n      \"font\": ");
                AppendJsonString(sb, pair.Key);
                sb.Append(",\n      \"missing\": [");
                int missingIndex = 0;
                foreach (uint cp in pair.Value)
                {
                    sb.Append(missingIndex++ == 0 ? "" : ", ")
                      .Append('"').Append("U+").Append(cp.ToString("X4")).Append('"');
                }
                sb.Append("],\n      \"findings\": [");

                var findings = Findings.Where(f => f.Font == pair.Key)
                    .OrderBy(f => f.Source).ThenBy(f => f.Location).ToArray();
                for (int j = 0; j < findings.Length; j++)
                {
                    var finding = findings[j];
                    sb.Append(j == 0 ? "\n" : ",\n")
                      .Append("        { \"source\": ");
                    AppendJsonString(sb, finding.Source);
                    sb.Append(", \"location\": ");
                    AppendJsonString(sb, finding.Location);
                    sb.Append(", \"missing\": ");
                    AppendJsonString(sb, string.Concat(finding.Missing.Select(cp => char.ConvertFromUtf32((int)cp))));
                    sb.Append(" }");
                }
                if (findings.Length > 0) sb.Append('\n').Append("      ");
                sb.Append("]\n    }");
            }
            if (fonts.Length > 0) sb.Append('\n').Append("  ");
            sb.Append("]\n}");
            return sb.ToString();
        }

        static void AppendJsonString(StringBuilder sb, string value)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        public string ToGitHubAnnotations()
        {
            var sb = new StringBuilder();
            foreach (var finding in Findings.OrderBy(f => f.Source).ThenBy(f => f.Location).ThenBy(f => f.Font))
            {
                int line = 0;
                bool hasLine = finding.Location != null
                    && finding.Location.StartsWith("line ", StringComparison.Ordinal)
                    && int.TryParse(finding.Location.Substring(5), out line)
                    && line > 0;
                sb.Append("::error file=").Append(EscapeAnnotationProperty(finding.Source));
                if (hasLine) sb.Append(",line=").Append(line);
                sb.Append(",title=Missing glyphs::");
                string message = finding.Font + " cannot draw: "
                    + string.Join(" ", finding.Missing.Select(cp => char.ConvertFromUtf32((int)cp)));
                if (!hasLine && !string.IsNullOrEmpty(finding.Location))
                    message += " (at " + finding.Location + ")";
                sb.Append(EscapeAnnotationMessage(message));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static string EscapeAnnotationProperty(string value) =>
            EscapeAnnotationMessage(value).Replace(":", "%3A").Replace(",", "%2C");

        static string EscapeAnnotationMessage(string value) =>
            (value ?? "").Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A");

        public string ToText()
        {
            var sb = new StringBuilder();
            sb.Append("TMP Glyph Audit: ").Append(TextsScanned).Append(" texts scanned, ");
            if (Clean) return sb.Append("no missing glyphs.").ToString();
            // A text with <font> tags can have one finding per font.
            sb.Append(Findings.Select(f => (f.Source, f.Location)).Distinct().Count()).Append(" with missing glyphs.\n");
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
