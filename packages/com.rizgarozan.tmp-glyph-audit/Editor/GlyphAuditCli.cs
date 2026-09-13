using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RizgarOzan.TmpGlyphAudit.Editor
{
    /// <summary>
    /// Batch-mode entry point for CI:
    /// <c>Unity -batchmode -projectPath . -executeMethod RizgarOzan.TmpGlyphAudit.Editor.GlyphAuditCli.Run -glyphAuditReport Logs/glyphs.txt</c>
    /// Exits with 0 when every text can be drawn and 1 when glyphs are missing.
    /// </summary>
    public static class GlyphAuditCli
    {
        public static void Run()
        {
            int exitCode = 2;
            try
            {
                var report = GlyphAudit.Run(GlyphAuditSettings.Load());
                string path = Argument("-glyphAuditReport") ?? "Logs/tmp-glyph-audit.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllText(path, report.ToText());
                Debug.Log(report.ToText());
                exitCode = report.Clean ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            EditorApplication.Exit(exitCode);
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
