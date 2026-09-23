using Makekoi.PartnerCreate;
using UnityEngine;

// リザルトのスコア内訳。買い物結果と所持金から算出する。
public sealed class ShopResultScore
{
    /// <summary>値引きが100%だった場合に1部位でもらえる点。</summary>
    public const int DiscountBaseScore = 1000;
    /// <summary>グレード点 (Budget 0 / Mid 50 / Premium 100) に掛ける倍率。</summary>
    public const int GradeBonusMultiplier = 50;
    /// <summary>お気に入り商品をチャレンジで勝ち取った場合のボーナス。</summary>
    public const int WishBonusScore = 500;

    /// <summary>値引き率スコアの合計。</summary>
    public readonly int PartScore;
    /// <summary>使い切らずに残った所持金。</summary>
    public readonly int MoneyLeftScore;
    /// <summary>グレードボーナスの合計。</summary>
    public readonly int GradeBonus;
    /// <summary>お気に入りボーナスの合計。</summary>
    public readonly int WishBonus;

    public int Total => PartScore + MoneyLeftScore + GradeBonus + WishBonus;

    private ShopResultScore(int partScore, int moneyLeftScore, int gradeBonus, int wishBonus)
    {
        PartScore = partScore;
        MoneyLeftScore = moneyLeftScore;
        GradeBonus = gradeBonus;
        WishBonus = wishBonus;
    }

    /// <summary>定価からどれだけ値引けたかの割合 × 1000。</summary>
    public static int GetDiscountScore(MiniGameResultData result)
    {
        if (result == null || result.BasePrice <= 0) return 0;
        var discountRate = Mathf.Clamp01((float)result.DiscountAmount / result.BasePrice);
        return Mathf.RoundToInt(DiscountBaseScore * discountRate);
    }

    public static int GetGradeScore(PartGrade grade) => grade switch
    {
        PartGrade.Premium => 100,
        PartGrade.Mid => 50,
        _ => 0,
    };

    public static ShopResultScore Calculate()
    {
        var results = ShoppingModel.Instance.Results;

        var partScore = 0;
        var gradeBonus = 0;
        var wishBonus = 0;

        foreach (PartType partType in System.Enum.GetValues(typeof(PartType)))
        {
            if (!results.TryGetValue(partType, out var result) || result == null || result.ResultPart == null)
            {
                continue;
            }

            partScore += GetDiscountScore(result);
            gradeBonus += GetGradeScore(result.ResultPart.Grade) * GradeBonusMultiplier;

            // お気に入りチャレンジに勝って、目玉商品のまま買えた部位だけ加点する
            if (ShoppingModel.Instance.IsFavoriteAchieved(partType))
            {
                wishBonus += WishBonusScore;
            }
        }

        var moneyLeft = Mathf.Max(0, GameModel.Instance.SessionData.Money);
        return new ShopResultScore(partScore, moneyLeft, gradeBonus, wishBonus);
    }
}
