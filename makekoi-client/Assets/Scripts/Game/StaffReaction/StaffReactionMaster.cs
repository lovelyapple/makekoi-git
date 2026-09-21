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
