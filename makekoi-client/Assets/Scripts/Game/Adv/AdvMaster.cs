using System.Linq;
using Makekoi.PartnerCreate;
using UnityEngine;

[System.Serializable]
public class AdvMaster
{
    public string Code;
    public int Gender; // GenderType: Female=0, Male=1
    public int ScriptIndex;
    public int CharaEmotionType;
    public string Message;
}

[CreateAssetMenu(fileName = "AdvMaster", menuName = "Scriptable Objects/AdvMaster")]
public class AdvTable : ScriptableObject
{
    public AdvMaster[] Data;

    public AdvMaster[] GetRecords(string code, GenderType gender)
    {
        return Data
            .Where(x => x.Code == code && x.Gender == (int)gender)
            .OrderBy(x => x.ScriptIndex)
            .ToArray();
    }
}

public static class AdvTableContainer
{
    public const string CharacterGenderSelectConfirm = "GENDER_SELECT_CONFIRM";
    private static AdvTable _master;

    public static AdvTable Master
    {
        get
        {
            if (_master == null)
                _master = Resources.Load<AdvTable>("Master/AdvMaster");
            return _master;
        }
    }

    public static AdvMaster[] GetRecords(string code, GenderType gender)
    {
        return Master?.GetRecords(code, gender) ?? System.Array.Empty<AdvMaster>();
    }
}
