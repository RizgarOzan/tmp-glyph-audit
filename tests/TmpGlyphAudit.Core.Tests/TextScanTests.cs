using RizgarOzan.TmpGlyphAudit;

public class TextScanTests
{
    static string Scan(string text, bool richText = true, bool parseEscapes = true) =>
        string.Concat(TextScan.CodePoints(text, richText, parseEscapes).Select(cp => char.ConvertFromUtf32((int)cp)));

    [Fact]
    public void Plain_text_is_every_character()
    {
        Assert.Equal("Şeker", Scan("Şeker"));
    }

    [Fact]
    public void Known_rich_text_tags_are_not_drawn()
    {
        Assert.Equal("Çok iyi", Scan("<b>Çok</b> <color=#FF0000>iyi</color>"));
        Assert.Equal("ab", Scan("<size=120%>a</size><sprite=3><#00FF00>b"));
        Assert.Equal("a b", Scan("<link=\"id\">a</link> <font=\"Bangers SDF\">b</font>"));
    }

    [Fact]
    public void Unknown_tags_and_lone_brackets_are_drawn_literally()
    {
        Assert.Equal("<madeup>x", Scan("<madeup>x"));
        Assert.Equal("a < b > c", Scan("a < b > c"));
        Assert.Equal("<b", Scan("<<b>b")); // first '<' is literal, "<b>" is a tag
    }

    [Fact]
    public void Tags_are_literal_when_rich_text_is_off()
    {
        Assert.Equal("<b>x</b>", Scan("<b>x</b>", richText: false));
    }

    [Fact]
    public void Noparse_draws_its_content_literally()
    {
        Assert.Equal("<b>x</b>y", Scan("<noparse><b>x</b></noparse><i>y</i>"));
    }

    [Fact]
    public void Escape_sequences_name_the_character_they_draw()
    {
        Assert.Equal("ğ", Scan(@"\u011F"));
        Assert.Equal("😀", Scan(@"\U0001F600"));
        Assert.Equal("ab", Scan(@"a\nb"));
        Assert.Equal(@"\u01", Scan(@"\u01"));       // too short: literal
        Assert.Equal(@"\u011F", Scan(@"\u011F", parseEscapes: false));
    }

    [Fact]
    public void Surrogate_pairs_are_one_code_point()
    {
        Assert.Equal(new uint[] { 0x1F600 }, TextScan.CodePoints("😀").ToArray());
    }

    [Fact]
    public void Characters_tmp_never_looks_up_are_skipped()
    {
        Assert.Equal("ab", Scan("a\u200Bb\u00AD\uFEFF\r\n\t"));
    }

    [Fact]
    public void Distinct_is_sorted_and_unique()
    {
        Assert.Equal(new uint[] { 'a', 'b' }, TextScan.Distinct("baab").ToArray());
    }

    [Fact]
    public void Tag_longer_than_tmp_limit_is_literal()
    {
        string longTag = "<link=\"" + new string('x', 130) + "\">";
        Assert.StartsWith("<link", Scan(longTag + "y"));
    }
}
