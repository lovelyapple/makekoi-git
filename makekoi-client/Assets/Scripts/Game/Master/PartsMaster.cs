using Makekoi.PartnerCreate;

[System.Serializable]
public class PartsMaster
{
    public PartType PartType;
    public int Gender; // GenderType: Female=0, Male=1
    public int Grade;  // PartGrade: Premium=0, Mid=1, Budget=2
    public string Name;
    public string TexturePath;
    public int Price; // 定価。ミニゲームの交渉でここから値引きする
}
