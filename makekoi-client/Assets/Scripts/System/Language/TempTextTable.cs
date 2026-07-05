using System.Linq;
using UnityEngine;

[System.Serializable]
public class TempTextData
{
    public string Title;
    public string Txt;
}

[CreateAssetMenu(fileName = "TempTextTable", menuName = "Scriptable Objects/TempTextTable")]
public class TempTextTable : ScriptableObject
{
    public TempTextData[] Data;
}

public static class TempTextTableContainer
{
    private static TempTextTable _table;

    public static TempTextTable Table
    {
        get
        {
            if (_table == null)
                _table = Resources.Load<TempTextTable>("Master/TempStringMaster");
            return _table;
        }
    }

    public static string GetText(string title)
    {
        return Table?.Data.FirstOrDefault(x => x.Title == title)?.Txt;
    }
}
