using System.Linq;
using UnityEngine;

public enum StaffReactionCode
{
    Welcome_1,
    Welcome_2,
    Idle,
    RequestKowtow,
    Happy,
    Surprised,
    Disappointed,
}

[System.Serializable]
public class StaffReactionMaster
{
    public StaffReactionCode Code;
    public int PatternCode;
    public string TexturePath;
    public string Message;
}

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
