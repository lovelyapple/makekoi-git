using System;
using System.Collections.Generic;
using System.IO;
using Makekoi.PartnerCreate;
using UnityEditor;
using UnityEngine;

public class PartsMasterImporter
{
    private const string TsvPath = "Assets/Scripts/Game/Master/Editor/parts_master_source.tsv";
    private const string OutputPath = "Assets/Resources/Master/PartsMaster.asset";

    [MenuItem("Makekoi/PartsMaster/Build")]
    public static void Build()
    {
        var dataList = new List<PartsMaster>();

        var lines = File.ReadAllLines(TsvPath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var columns = line.Split('\t');
            if (columns.Length < 5)
                continue;

            var record = new PartsMaster
            {
                PartType = (PartType)Enum.Parse(typeof(PartType), columns[0].Trim()),
                Gender = int.Parse(columns[1].Trim()),
                Grade = int.Parse(columns[2].Trim()),
                Name = columns[3].Trim(),
                TexturePath = columns[4].Trim(),
            };
            dataList.Add(record);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Master"))
            AssetDatabase.CreateFolder("Assets/Resources", "Master");

        var master = AssetDatabase.LoadAssetAtPath<PartsTable>(OutputPath);
        if (master == null)
        {
            master = ScriptableObject.CreateInstance<PartsTable>();
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

        Debug.Log($"[PartsMasterImporter] {dataList.Count} entries → {OutputPath}");
    }
}
