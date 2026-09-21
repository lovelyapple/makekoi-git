using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "StaffReactionMaster", menuName = "Scriptable Objects/StaffReactionMaster")]
public class StaffReactionTable : ScriptableObject
{
    public StaffReactionMaster[] Data;

    public StaffReactionMaster GetRandomRecord(StaffReactionCode code)
    {
        var candidates = Data.Where(x => x.Code == code).ToArray();
        if (candidates.Length == 0) return null;
        return candidates[Random.Range(0, candidates.Length)];
    }
}

public static class StaffReactionTableContainer
{
    private static StaffReactionTable _master;

    public static StaffReactionTable Master
    {
        get
        {
            if (_master == null)
                _master = Resources.Load<StaffReactionTable>("Master/StaffReactionMaster");
            return _master;
        }
    }

    public static StaffReactionMaster GetRandomRecord(StaffReactionCode code)
    {
        return Master?.GetRandomRecord(code);
    }
}
