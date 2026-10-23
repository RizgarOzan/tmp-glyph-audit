using RizgarOzan.TmpGlyphAudit;

public class AuditReportTests
{
    static Finding Finding(string source, string location, string font, params uint[] missing) =>
        new()
        {
            Source = source,
            Location = location,
            Font = font,
            Missing = new SortedSet<uint>(missing)
        };

    [Fact]
    public void Clean_is_true_without_findings_and_false_with_one()
    {
        var report = new AuditReport();

        Assert.True(report.Clean);

        report.Findings.Add(Finding("Assets/Menu.prefab", "Canvas/Title", "Main SDF", 'x'));

        Assert.False(report.Clean);
    }

    [Fact]
    public void MissingByFont_merges_code_points_for_the_same_font_and_keeps_fonts_separate()
    {
        var report = new AuditReport();
        report.Findings.Add(Finding("Assets/A.prefab", "Title", "Main SDF", 'b', 'a'));
        report.Findings.Add(Finding("Assets/B.prefab", "Label", "Main SDF", 'c', 'b'));
        report.Findings.Add(Finding("Assets/C.prefab", "Label", "Digits SDF", '%'));

        var byFont = report.MissingByFont();

        Assert.Equal(new uint[] { 'a', 'b', 'c' }, byFont["Main SDF"].ToArray());
        Assert.Equal(new uint[] { '%' }, byFont["Digits SDF"].ToArray());
        Assert.Equal(2, byFont.Count);
    }

    [Fact]
    public void CharacterSet_includes_bmp_and_non_bmp_characters_and_is_empty_without_findings()
    {
        var report = new AuditReport();
        report.Findings.Add(Finding("Assets/Menu.prefab", "Canvas/Title", "Main SDF", 0x1F600, 'ğ', 'A'));

        Assert.Equal("Ağ😀", report.CharacterSet("Main SDF"));
        Assert.Equal("", report.CharacterSet("Font without findings"));
    }

    [Fact]
    public void ToText_clean_report_ends_with_no_missing_glyphs()
    {
        var report = new AuditReport { TextsScanned = 5 };

        Assert.EndsWith("no missing glyphs.", report.ToText());
    }

    [Fact]
    public void ToText_lists_font_before_findings_sorted_by_source_then_location()
    {
        var report = new AuditReport { TextsScanned = 3 };
        report.Findings.Add(Finding("Assets/Z.prefab", "Canvas/Title", "Main SDF", 'z'));
        report.Findings.Add(Finding("Assets/A.prefab", "Canvas/Z", "Main SDF", 'y'));
        report.Findings.Add(Finding("Assets/A.prefab", "Canvas/A", "Main SDF", 'x'));

        string text = report.ToText();
        int fontLine = text.IndexOf("Main SDF is missing", StringComparison.Ordinal);
        int firstFinding = text.IndexOf("Assets/A.prefab > Canvas/A: x", StringComparison.Ordinal);
        int secondFinding = text.IndexOf("Assets/A.prefab > Canvas/Z: y", StringComparison.Ordinal);
        int thirdFinding = text.IndexOf("Assets/Z.prefab > Canvas/Title: z", StringComparison.Ordinal);

        Assert.True(fontLine >= 0);
        Assert.True(firstFinding > fontLine);
        Assert.True(secondFinding > firstFinding);
        Assert.True(thirdFinding > secondFinding);
    }
}
