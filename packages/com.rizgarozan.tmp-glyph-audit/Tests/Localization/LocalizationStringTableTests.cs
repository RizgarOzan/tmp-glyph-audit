using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RizgarOzan.TmpGlyphAudit.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public class LocalizationStringTableTests
{
    const string Folder = "Assets/__TmpGlyphAuditLocalizationTests";
    const string LiberationSansPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [SetUp]
    public void SetUp()
    {
        if (AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.DeleteAsset(Folder);
        AssetDatabase.CreateFolder("Assets", "__TmpGlyphAuditLocalizationTests");
    }

    [TearDown]
    public void TearDown()
    {
        if (AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.DeleteAsset(Folder);
        AssetDatabase.Refresh();
    }

    [Test]
    public void Scans_locale_tables_and_reports_missing_japanese_glyphs_by_key()
    {
        var shared = ScriptableObject.CreateInstance<SharedTableData>();
        shared.TableCollectionName = "GlyphAuditLocalizationTest";
        AssetDatabase.CreateAsset(shared, Folder + "/Shared.asset");

        CreateTable("Turkish", "tr", shared, "greeting", "Merhaba");
        CreateTable("Japanese", "ja", shared, "welcome", "こんにちは");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var settings = new GlyphAuditSettings
        {
            folders = new List<string>(),
            scanScenes = false,
            scanPrefabs = false,
            localizationFonts = new List<GlyphAuditSettings.LocaleFont>
            {
                new GlyphAuditSettings.LocaleFont { localeCode = "tr", fontAsset = LiberationSansPath },
                new GlyphAuditSettings.LocaleFont { localeCode = "ja", fontAsset = LiberationSansPath },
            },
        };

        var report = GlyphAudit.Run(settings);
        var finding = report.Findings.Single(f => f.Source == Folder + "/Japanese.asset");

        Assert.AreEqual("welcome", finding.Location);
        Assert.AreEqual(LiberationSansPath, finding.Font);
        Assert.IsFalse(report.Findings.Any(f => f.Source == Folder + "/Turkish.asset"));
    }

    static void CreateTable(string assetName, string localeCode, SharedTableData shared, string key, string value)
    {
        var table = ScriptableObject.CreateInstance<StringTable>();
        table.name = assetName;
        table.SharedData = shared;
        table.LocaleIdentifier = new LocaleIdentifier(localeCode);
        AssetDatabase.CreateAsset(table, Folder + "/" + assetName + ".asset");
        table.AddEntry(key, value);
        EditorUtility.SetDirty(table);
    }
}
