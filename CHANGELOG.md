# Changelog

All notable changes to `com.rizgarozan.tmp-glyph-audit`. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow
[Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.3.0] - 2026-10-20

All three by [@Aaqibhafeezkhan](https://github.com/Aaqibhafeezkhan), the package's first outside contributor.

### Added
- JSON report for CI, chosen by giving `-glyphAuditReport` a `.json` path: scan counts, missing
  code points per font, and every finding with its source and location (#10).
- `-glyphAuditAnnotations`: one GitHub Actions `::error` annotation per finding, shown on the
  workflow run and the pull request (#11).
- Unity Localization string tables, scanned against a TMP font asset configured per locale;
  Smart String placeholders such as `{0}` are skipped. Optional: without `com.unity.localization`
  the package still compiles (#12).

## [0.2.0] - 2026-09-15

### Added
- `<font="Name">` tags: text inside is checked against the font the tag loads (Resources
  under the TMP Settings font path, then any TMP font asset of that name) and reported under
  it; a tag naming a font that does not exist is checked as the literal text TMP draws.

## [0.1.0] - 2026-09-14

### Added
- Scan `TMP_Text` in scenes and prefabs, plus runtime text files, for characters their
  font chain cannot draw, using TMP's own fallback order.
- **Window → Text → TMP Glyph Audit**, with per-font export of missing characters for the
  Font Asset Creator.
- Batch-mode entry point `GlyphAuditCli.Run` with exit codes for CI.

[Unreleased]: https://github.com/RizgarOzan/tmp-glyph-audit/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/RizgarOzan/tmp-glyph-audit/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/RizgarOzan/tmp-glyph-audit/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/RizgarOzan/tmp-glyph-audit/releases/tag/v0.1.0
