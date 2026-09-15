# Contributing

Thanks for helping. Small pull requests are welcome, and most issues here can be done
without Unity.

## Pick something

- [`good first issue`](https://github.com/RizgarOzan/tmp-glyph-audit/labels/good%20first%20issue):
  each one says what to change and how the result is checked.
- Found a text the audit gets wrong (a glyph it misses, or one it reports that TMP draws
  fine)? Open an issue with the text, the font asset and its fallbacks. That is the most
  useful bug report this project can get.

Comment on the issue before you start so two people don't do the same work.

## Where things live

| Folder | What | Needs |
|---|---|---|
| `packages/com.rizgarozan.tmp-glyph-audit/Core/` | text scanning, fallback resolution, the report; plain C#, no Unity reference | .NET SDK |
| `packages/com.rizgarozan.tmp-glyph-audit/Editor/` | reading TMP assets, the window, the batch-mode entry point | Unity |
| `src/TmpGlyphAudit.Core/` | .NET project that compiles the package's `Core/*.cs` as they are | .NET SDK |
| `tests/TmpGlyphAudit.Core.Tests/` | xUnit tests for `Core/` | .NET SDK |
| `unity/` | Unity 6000.3 test project with the EditMode tests | Unity |

## Check your change

```bash
dotnet test tests/TmpGlyphAudit.Core.Tests
```

CI runs this on every pull request. If you touched `Editor/`, also run the EditMode tests
in `unity/` (Unity Test Runner, or `unity test unity --mode EditMode` with the Unity CLI) and
say in the PR that you did; CI can't run Unity.

A behaviour change needs a test that fails without it. When the change is about how TMP
draws something, point to the TMP source line (in `com.unity.ugui`) the behaviour comes from.

## Pull request

- One topic per PR, with a line in `CHANGELOG.md` under *Unreleased* if users would notice.
- Keep `Core/` free of `UnityEngine`/`UnityEditor` references; it must build under .NET.
- By contributing you agree your code is released under the [MIT license](LICENSE).
