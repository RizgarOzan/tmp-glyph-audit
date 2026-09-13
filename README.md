# TMP Glyph Audit

[![tests](https://github.com/RizgarOzan/tmp-glyph-audit/actions/workflows/tests.yml/badge.svg)](https://github.com/RizgarOzan/tmp-glyph-audit/actions/workflows/tests.yml)

Find every TextMeshPro text your fonts **cannot draw** — before a player sees the
empty boxes.

TextMeshPro only tells you a character is missing when that exact text is rendered, one
warning at a time, at runtime. That is how a Turkish `ğ`, a Polish `ł` or a Japanese menu
ships as `□□□`. This package checks the whole project at once, in the editor or in CI:

- every `TMP_Text` in your **scenes** and **prefabs**,
- **runtime-only text** — localization exports, dialogue files — against the font that
  will display it,
- through the **same fallback chain TMP uses**: the font, its fallback list (depth
  first), the global fallbacks in TMP Settings, then the TMP Settings default font.
  Dynamic font assets count a character as available when their source font file has it,
  because TMP adds it to the atlas at runtime.

It never modifies a font asset or its atlas.

```
TMP Glyph Audit: 5 texts scanned, 2 with missing glyphs.

Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset is missing 15: U+3046 'う', U+3053 'こ', …
  Assets/Localization/ja.txt > line 1: うこそへよゲムー！
  Assets/Localization/ja.txt > line 2: して了保存終
```

*(Real output from the test project in this repository: `tr.txt` passes — LiberationSans
covers Turkish — and `ja.txt` does not.)*

## Install

Unity 2023.2 or newer (TextMeshPro inside `com.unity.ugui` 2.0). In **Package Manager →
+ → Add package from git URL**:

```
https://github.com/RizgarOzan/tmp-glyph-audit.git?path=/packages/com.rizgarozan.tmp-glyph-audit
```

## Use

**Window → Text → TMP Glyph Audit.** Choose folders, tick scenes / prefabs, add runtime
text files (folder + pattern + the TMP font that shows them) and press **Scan**. For each
font you get the missing characters and every text that needs them; **Ping** selects the
asset, **Export characters…** writes the missing set to a `.txt` you can feed to the Font
Asset Creator (*Character Set → Characters from File*) to build a fallback atlas.

Settings are saved to `ProjectSettings/TmpGlyphAuditSettings.json`, so the team and CI
share them.

**CI:** run the same scan in batch mode. Exit code `0` = every character can be drawn,
`1` = glyphs are missing, `2` = the scan itself failed.

```bash
Unity -batchmode -nographics -projectPath . \
  -executeMethod RizgarOzan.TmpGlyphAudit.Editor.GlyphAuditCli.Run \
  -glyphAuditReport Logs/tmp-glyph-audit.txt
```

## What counts as "needs a glyph"

The scanner reads text the way TMP's parser does: rich-text tags (`<b>`, `<color=#F00>`,
`<sprite=3>`, …) are not drawn, `<noparse>` content is drawn literally, `ğ`-style
escapes draw the character they name, surrogate pairs are one character, and control,
zero-width and bidi characters are skipped. Unknown tags such as `<madeup>` are drawn
literally — TMP does the same.

## Limits

- A `<font="…">` tag switches font mid-text in TMP; the audit checks the whole text against
  the component's own font, so text inside such a tag can be reported as missing.
- **Unity Localization string tables** are not read yet — export them to text or help add
  it (see issues).
- `DynamicOS` font assets are checked against the fonts installed on the machine running the
  audit, which may differ from your players'.

## Develop

The rules (text scanning, fallback resolution, the report) are plain C# with no Unity
reference, compiled both into the package and into a .NET project:

```bash
dotnet test tests/TmpGlyphAudit.Core.Tests      # 18 tests, no Unity needed
```

The Unity side is tested in `unity/` (Unity 6000.3, TMP Essential Resources imported):

```bash
unity test unity --mode EditMode                 # 4 tests, uses a generated test font
```

`tools/make_test_font.py` builds the tiny font the Unity tests use (fontTools), so the repo
carries no third-party font of its own.

## License

MIT — see [LICENSE](LICENSE). TMP Essential Resources inside `unity/` belong to Unity and
come with its TextMeshPro package.
