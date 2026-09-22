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
    // ミニゲーム結果（獲得グレード別のガンマの反応）
    Result_Premium,
    Result_Mid,
    Result_Budget,
    // 値段交渉ミニゲーム専用
    TimeTap_Idle,
    TimeTap_Request,
    TimeTap_Success,
    TimeTap_Failure,
    // 所持金が尽きて買い物を続けられなくなったとき
    MiniGame_Failed,
}

[System.Serializable]
public class StaffReactionMaster
{
    public StaffReactionCode Code;
    public int PatternCode;
    public string TexturePath;
    public string Message;
}
