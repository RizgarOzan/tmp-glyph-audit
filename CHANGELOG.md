# Changelog

All notable changes to `com.rizgarozan.tmp-glyph-audit`. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow
[Semantic Versioning](https://semver.org/).

## [Unreleased]

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
