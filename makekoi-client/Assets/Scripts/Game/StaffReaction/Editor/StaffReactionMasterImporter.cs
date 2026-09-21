using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;

public class StaffReactionMasterImporter
{
    private const string TsvPath = "Assets/Scripts/Game/StaffReaction/Editor/staff_reaction_master_source.tsv";
    private const string OutputPath = "Assets/Resources/Master/StaffReactionMaster.asset";

    [MenuItem("Makekoi/StaffReactionMaster/Build")]
    public static void Build()
    {
        var dataList = new List<StaffReactionMaster>();
        // 主キーは (Code, PatternCode) の複合キー。Code の重複はバリエーションとして正常だが、
        // PatternCode まで重複するとその1行だけ抽選確率が上がってしまうため検出する
        var usedKeys = new HashSet<(StaffReactionCode, int)>();

        var lines = File.ReadAllLines(TsvPath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var columns = line.Split('\t');
            if (columns.Length < 4)
                continue;

            var record = new StaffReactionMaster
            {
                Code = (StaffReactionCode)Enum.Parse(typeof(StaffReactionCode), columns[0].Trim()),
                PatternCode = int.Parse(columns[1].Trim()),
                TexturePath = columns[2].Trim(),
                Message = columns[3].Trim()
                    .Replace("\\n", "\n")
                    .Replace("¥n", "\n"),
            };
            if (!usedKeys.Add((record.Code, record.PatternCode)))
            {
                Debug.LogError($"[StaffReactionMasterImporter] 主キー重複: {record.Code} / PatternCode={record.PatternCode}");
            }

            dataList.Add(record);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Master"))
            AssetDatabase.CreateFolder("Assets/Resources", "Master");

        var master = AssetDatabase.LoadAssetAtPath<StaffReactionTable>(OutputPath);
        if (master == null)
        {
            master = ScriptableObject.CreateInstance<StaffReactionTable>();
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

        Debug.Log($"[StaffReactionMasterImporter] {dataList.Count} entries → {OutputPath}");
    }
}
