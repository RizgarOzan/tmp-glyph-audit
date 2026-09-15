using System.IO;
using System.Linq;
using RizgarOzan.TmpGlyphAudit.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Renders docs/missing-glyphs.png for the README: every line of the sample text files,
// drawn by TextMeshPro with the font the audit settings assign to it, and the audit's own
// summary line under them. Needs a graphics device, so no -nographics:
//   unity run unity -- -executeMethod ReadmeImage.Render
static class ReadmeImage
{
    const int Width = 1100, Height = 620;

    public static void Render()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var settings = GlyphAuditSettings.Load();
        var source = settings.textSources[0];
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(source.fontAsset);
        var gray = new Color32(139, 147, 161, 255);

        float y = 50;
        foreach (string file in Directory.GetFiles(source.folder, source.pattern).OrderByDescending(f => f))
        {
            string path = file.Replace('\\', '/');
            Text($"{path}  ·  {font.name}", font, 24, gray, ref y);
            foreach (string line in File.ReadLines(path).Where(l => l.Length > 0))
                Text(line, font, 52, Color.white, ref y);
            y += 30;
        }
        string summary = GlyphAudit.Run(settings).ToText().Split('\n')[0];
        Text(summary, font, 30, new Color32(255, 196, 87, 255), ref y);

        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Height / 20f;   // 1 world unit = 10 px
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(30, 31, 36, 255);
        camera.transform.position = new Vector3(0, 0, -10);
        camera.targetTexture = rt;
        camera.Render();

        RenderTexture.active = rt;
        var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        string output = Path.GetFullPath("../docs/missing-glyphs.png");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllBytes(output, image.EncodeToPNG());
        Debug.Log($"ReadmeImage: wrote {output}");
        EditorApplication.Exit(0);
    }

    static void Text(string value, TMP_FontAsset font, float size, Color color, ref float y)
    {
        var text = new GameObject(value).AddComponent<TextMeshPro>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.rectTransform.pivot = new Vector2(0, 1);
        text.rectTransform.sizeDelta = new Vector2((Width - 120) / 10f, size / 5f);
        text.rectTransform.position = new Vector3(-Width / 20f + 6, Height / 20f - y / 10f, 0);
        text.text = value;
        text.ForceMeshUpdate();
        y += size * 1.35f;
    }
}
