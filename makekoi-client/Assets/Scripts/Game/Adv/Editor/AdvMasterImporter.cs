using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AdvMasterImporter
{
    private const string TsvPath = "Assets/Scripts/Game/Adv/Editor/adv_master_source.tsv";
    private const string OutputPath = "Assets/Resources/Master/AdvMaster.asset";

    [MenuItem("Makekoi/AdvMaster/Build")]
    public static void Build()
    {
        var dataList = new List<AdvMasterRecord>();

        var lines = File.ReadAllLines(TsvPath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var columns = line.Split('\t');
            if (columns.Length < 5)
                continue;

            var record = new AdvMasterRecord
            {
                Code = columns[0].Trim(),
                Gender = int.Parse(columns[1].Trim()),
                ScriptIndex = int.Parse(columns[2].Trim()),
                CharaEmotionType = int.Parse(columns[3].Trim()),
                Message = columns[4].Trim()
                    .Replace("\\n", "\n")
                    .Replace("¥n", "\n"),
            };
            dataList.Add(record);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Master"))
            AssetDatabase.CreateFolder("Assets/Resources", "Master");

        var master = AssetDatabase.LoadAssetAtPath<AdvMaster>(OutputPath);
        if (master == null)
        {
            master = ScriptableObject.CreateInstance<AdvMaster>();
            master.Data = dataList.ToArray();
            AssetDatabase.CreateAsset(master, OutputPath);
        }
        else
        {
            master.Data = dataList.ToArray();
            EditorUtility.SetDirty(master);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AdvMasterImporter] {dataList.Count} entries → {OutputPath}");
    }
}
