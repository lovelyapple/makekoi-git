/// <summary>
/// SoundsContainer の BgmResources に登録したキー。
/// SE (CommonSeType) と違って enum の自動生成が無いので、登録したら手で足す。
/// 曲そのものの差し替えは SoundsContainer 側でできる。
/// </summary>
public static class CommonBgmType
{
    /// <summary>タイトルからキャラ選択まで。</summary>
    public const string PhaseMain = "phase_main";

    /// <summary>買い物 (値引き交渉) 中。</summary>
    public const string PhaseShopping = "phase_shopping";

    /// <summary>最終結果。</summary>
    public const string PhaseResult = "phase_result";
}
