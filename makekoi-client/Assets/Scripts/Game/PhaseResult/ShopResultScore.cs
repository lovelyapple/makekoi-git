using Makekoi.PartnerCreate;
using UnityEngine;

/// <summary>デート相手のリアクションを出し分けるための出来ばえ。</summary>
public enum ShopResultRank
{
    Bad,
    Normal,
    Good,
    Great,
}

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

    // ランクの境目。理論値に対する達成率で見る。
    private const float GreatRate = 0.80f;
    private const float GoodRate = 0.60f;
    private const float NormalRate = 0.35f;

    /// <summary>
    /// 全部位を最上位グレードで買い、値引きも上限まで決め、お気に入りも取り切った場合の理論値。
    /// 残高点は青天井なので含めない (7部位なら 7 * 5500 + 500 = 39000)。
    /// </summary>
    public static int ReferenceScore
    {
        get
        {
            var partCount = System.Enum.GetValues(typeof(PartType)).Length;
            var perPart = Mathf.RoundToInt(DiscountBaseScore * MiniGameBase.MaxDiscountRate)
                          + GetGradeScore(PartGrade.Premium) * GradeBonusMultiplier;
            return partCount * perPart + ShoppingModel.FavoritePartCount * WishBonusScore;
        }
    }

    /// <summary>理論値に対する達成率。残高点が乗るので 1 を超えることがある。</summary>
    public float AchievementRate => ReferenceScore <= 0 ? 0f : (float)Total / ReferenceScore;

    public ShopResultRank Rank
    {
        get
        {
            var rate = AchievementRate;
            if (rate >= GreatRate) return ShopResultRank.Great;
            if (rate >= GoodRate) return ShopResultRank.Good;
            if (rate >= NormalRate) return ShopResultRank.Normal;
            return ShopResultRank.Bad;
        }
    }

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
