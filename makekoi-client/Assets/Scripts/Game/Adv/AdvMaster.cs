using System.Linq;
using Makekoi.PartnerCreate;
using UnityEngine;

[System.Serializable]
public class AdvMasterRecord
{
    public string Code;
    public int Gender; // GenderType: Female=0, Male=1
    public int ScriptIndex;
    public int CharaEmotionType;
    public string Message;
}

[CreateAssetMenu(fileName = "AdvMaster", menuName = "Scriptable Objects/AdvMaster")]
public class AdvMaster : ScriptableObject
{
    public AdvMasterRecord[] Data;

    public AdvMasterRecord[] GetRecords(string code, GenderType gender)
    {
        return Data
            .Where(x => x.Code == code && x.Gender == (int)gender)
            .OrderBy(x => x.ScriptIndex)
            .ToArray();
    }
}

public static class AdvMasterContainer
{
    public const string CharacterGenderSelectConfirm = "GENDER_SELECT_CONFIRM";
    private static AdvMaster _master;

    public static AdvMaster Master
    {
        get
        {
            if (_master == null)
                _master = Resources.Load<AdvMaster>("Master/AdvMaster");
            return _master;
        }
    }

    public static AdvMasterRecord[] GetRecords(string code, GenderType gender)
    {
        return Master?.GetRecords(code, gender) ?? System.Array.Empty<AdvMasterRecord>();
    }
}
