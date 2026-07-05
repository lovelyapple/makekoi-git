using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class TempStringDataExporter
{
    private const string TsvPath = "Assets/Scripts/System/Language/Editor/string_source.tsv";
    private const string OutputPath = "Assets/Resources/Master/TempStringMaster.asset";
    private const string CurrentTextPath = "Assets/Fonts/Japanese/current_text.txt";
    private static readonly Regex KanjiRegex = new Regex(@"[\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]", RegexOptions.Compiled);

    [MenuItem("Makekoi/StringMaster/BuildString")]
    public static void BuildString()
    {
        var dataList = new List<TempTextData>();

        var lines = File.ReadAllLines(TsvPath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var columns = line.Split('\t');
            if (columns.Length < 3)
                continue;

            var title = columns[1].Trim();
            var txt = columns[2].Trim()
                .Replace("\\n", "\n")
                .Replace("¥n", "\n");

            if (string.IsNullOrEmpty(title))
                continue;

            dataList.Add(new TempTextData { Title = title, Txt = txt });
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Master"))
            AssetDatabase.CreateFolder("Assets/Resources", "Master");

        var table = AssetDatabase.LoadAssetAtPath<TempTextTable>(OutputPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<TempTextTable>();
            table.Data = dataList.ToArray();
            AssetDatabase.CreateAsset(table, OutputPath);
        }
        else
        {
            table.Data = dataList.ToArray();
            EditorUtility.SetDirty(table);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AppendMissingKanji(dataList);

        Debug.Log($"[TempStringDataExporter] {dataList.Count} entries → {OutputPath}");
    }

    private static void AppendMissingKanji(List<TempTextData> dataList)
    {
        if (!File.Exists(CurrentTextPath))
        {
            Debug.LogWarning($"[TempStringDataExporter] current_text.txt not found: {CurrentTextPath}");
            return;
        }

        var currentText = File.ReadAllText(CurrentTextPath);
        var existingChars = new HashSet<char>(currentText);

        var missing = new List<char>();
        foreach (var data in dataList)
        {
            foreach (Match match in KanjiRegex.Matches(data.Txt ?? string.Empty))
            {
                var c = match.Value[0];
                if (existingChars.Add(c))
                    missing.Add(c);
            }
        }

        if (missing.Count == 0)
        {
            Debug.Log("[TempStringDataExporter] No missing kanji found.");
            return;
        }

        var appendText = string.Concat(missing);
        File.AppendAllText(CurrentTextPath, "\n" + appendText);
        AssetDatabase.Refresh();
        Debug.Log($"[TempStringDataExporter] Added {missing.Count} kanji to {CurrentTextPath}: {appendText}");
    }
}
