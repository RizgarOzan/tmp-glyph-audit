"""Build the tiny font the Unity tests use: a square for each of a handful of
characters, nothing else. Generated rather than downloaded so the repository
carries no third-party font and the test knows exactly which glyphs exist.

    python tools/make_test_font.py
"""
from pathlib import Path

from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

CHARS = " ABCğ"  # the tests rely on exactly this set
OUT = Path(__file__).resolve().parent.parent / "packages/com.rizgarozan.tmp-glyph-audit/Tests/Editor/Fonts/GlyphAuditTest.ttf"


def square(filled: bool):
    pen = TTGlyphPen(None)
    if filled:
        pen.moveTo((100, 0)); pen.lineTo((100, 700)); pen.lineTo((500, 700)); pen.lineTo((500, 0)); pen.closePath()
    return pen.glyph()


names = [".notdef"] + [f"uni{ord(c):04X}" for c in CHARS]
fb = FontBuilder(1000, isTTF=True)
fb.setupGlyphOrder(names)
fb.setupCharacterMap({ord(c): f"uni{ord(c):04X}" for c in CHARS})
fb.setupGlyf({n: square(n not in (".notdef", "uni0020")) for n in names})
fb.setupHorizontalMetrics({n: (600, 100) for n in names})
fb.setupHorizontalHeader(ascent=800, descent=-200)
fb.setupNameTable({"familyName": "GlyphAuditTest", "styleName": "Regular"})
fb.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800, usWinDescent=200)
fb.setupPost()
OUT.parent.mkdir(parents=True, exist_ok=True)
fb.save(str(OUT))
print(OUT, OUT.stat().st_size, "bytes")
