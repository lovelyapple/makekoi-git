using System.Linq;
using Makekoi.PartnerCreate;
using UnityEngine;

[System.Serializable]
public class PartsMaster
{
    public PartType PartType;
    public int Gender; // GenderType: Female=0, Male=1
    public int Grade;  // PartGrade: Premium=0, Mid=1, Budget=2
    public string Name;
    public string TexturePath;
}

[CreateAssetMenu(fileName = "PartsMaster", menuName = "Scriptable Objects/PartsMaster")]
public class PartsTable : ScriptableObject
{
    public PartsMaster[] Data;

    public PartsMaster GetRecord(PartType partType, GenderType gender, PartGrade grade)
    {
        return Data.FirstOrDefault(x =>
            x.PartType == partType &&
            x.Gender == (int)gender &&
            x.Grade == (int)grade);
    }
}

public static class PartsTableContainer
{
    private static PartsTable _master;

    public static PartsTable Master
    {
        get
        {
            if (_master == null)
                _master = Resources.Load<PartsTable>("Master/PartsMaster");
            return _master;
        }
    }

    public static PartsMaster GetRecord(PartType partType, GenderType gender, PartGrade grade)
    {
        return Master?.GetRecord(partType, gender, grade);
    }
}
