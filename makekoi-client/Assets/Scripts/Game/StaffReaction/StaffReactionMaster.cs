public enum StaffReactionCode
{
    Welcome_1,
    Welcome_2,
    Idle,
    // 土下座ミニゲーム専用
    Dogeza_Request,
    Dogeza_Happy,
    Dogeza_Surprised,
    Dogeza_Disappointed,
}

[System.Serializable]
public class StaffReactionMaster
{
    public StaffReactionCode Code;
    public int PatternCode;
    public string TexturePath;
    public string Message;
}
