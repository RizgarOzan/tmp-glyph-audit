using RizgarOzan.TmpGlyphAudit;

public class GlyphResolverTests
{
    sealed class FakeFont : IGlyphSource
    {
        readonly HashSet<uint> _glyphs;
        public readonly List<IGlyphSource> FallbackList = new();
        public int Lookups;
        public FakeFont(string name, string glyphs)
        {
            Name = name;
            _glyphs = new HashSet<uint>(TextScan.CodePoints(glyphs, richText: false, parseEscapes: false));
        }
        public string Name { get; }
        public bool HasGlyph(uint cp) { Lookups++; return _glyphs.Contains(cp); }
        public IReadOnlyList<IGlyphSource> Fallbacks => FallbackList;
    }

    [Fact]
    public void Characters_in_the_font_are_not_missing()
    {
        var latin = new FakeFont("Latin", "abc ");
        Assert.Empty(new GlyphResolver().Missing(latin, "a b c"));
    }

    [Fact]
    public void Missing_lists_what_no_font_has()
    {
        var latin = new FakeFont("Latin", "Seker ");
        Assert.Equal(new uint[] { 'Ş' }, new GlyphResolver().Missing(latin, "Şeker").ToArray());
    }

    [Fact]
    public void Fallback_chain_is_searched_depth_first()
    {
        var turkish = new FakeFont("Turkish", "ğış");
        var cjk = new FakeFont("CJK", "漢");
        var mid = new FakeFont("Mid", "");
        mid.FallbackList.Add(cjk);
        var main = new FakeFont("Main", "abc");
        main.FallbackList.Add(mid);
        main.FallbackList.Add(turkish);
        Assert.Empty(new GlyphResolver().Missing(main, "a漢ğ"));
    }

    [Fact]
    public void Global_fallbacks_and_default_font_are_last_resort()
    {
        var main = new FakeFont("Main", "a");
        var global = new FakeFont("Global", "b");
        var dflt = new FakeFont("Default", "c");
        var resolver = new GlyphResolver(new[] { global }, dflt);
        Assert.Equal(new uint[] { 'd' }, resolver.Missing(main, "abcd").ToArray());
        Assert.Equal(new uint[] { 'b', 'c', 'd' }, new GlyphResolver().Missing(main, "abcd").ToArray());
    }

    [Fact]
    public void Fallback_cycles_terminate_and_search_each_font_once()
    {
        var a = new FakeFont("A", "x");
        var b = new FakeFont("B", "y");
        a.FallbackList.Add(b);
        b.FallbackList.Add(a);
        var resolver = new GlyphResolver(new IGlyphSource[] { a, b }, a);
        Assert.False(resolver.CanRender(a, 'z'));
        Assert.Equal(1, a.Lookups);
        Assert.Equal(1, b.Lookups);
    }

    [Fact]
    public void Null_entries_in_fallback_lists_are_skipped()
    {
        var main = new FakeFont("Main", "a");
        main.FallbackList.Add(null);
        Assert.True(new GlyphResolver(new IGlyphSource[] { null }).CanRender(main, 'a'));
    }

    [Fact]
    public void Report_groups_by_font_and_exports_a_character_set()
    {
        var report = new AuditReport { TextsScanned = 3 };
        report.Findings.Add(new Finding { Source = "Assets/Menu.unity", Location = "Canvas/Title", Font = "Main SDF", Missing = new SortedSet<uint> { 'ğ' } });
        report.Findings.Add(new Finding { Source = "Assets/Shop.prefab", Location = "Price", Font = "Main SDF", Missing = new SortedSet<uint> { 'ş', 'ğ' } });
        report.Findings.Add(new Finding { Source = "Assets/Hud.prefab", Location = "Score", Font = "Digits SDF", Missing = new SortedSet<uint> { '%' } });

        Assert.False(report.Clean);
        Assert.Equal("ğş", report.CharacterSet("Main SDF"));
        Assert.Equal("", report.CharacterSet("Unknown"));
        string text = report.ToText();
        Assert.Contains("3 texts scanned, 3 with missing glyphs", text);
        Assert.Contains("Main SDF is missing 2: U+011F 'ğ', U+015F 'ş'", text);
        Assert.Contains("  Assets/Shop.prefab > Price: ğş", text);
    }

    [Fact]
    public void Clean_report_says_so()
    {
        Assert.Equal("TMP Glyph Audit: 5 texts scanned, no missing glyphs.", new AuditReport { TextsScanned = 5 }.ToText());
    }
}
