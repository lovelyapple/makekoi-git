using System.Collections.Generic;
using Makekoi.PartnerCreate;
using R3;
using UnityEngine;
using UnityEngine.Analytics;
public enum MiniGameType
{
    Dogeza,
    Jyanken,
    TimeTap,
}
public sealed class MiniGameRequestData
{
    public readonly MiniGameType MiniGameType;
    public readonly PartType PartType;
    /// <summary>この部位で売られているパーツ。買い物開始時に抽選済み。</summary>
    public readonly PartnerPartData Part;
    /// <summary>交渉前の定価。</summary>
    public readonly long BasePrice;
    /// <summary>お気に入り商品への挑戦か。失敗するとグレードを下げて仕切り直しになる。</summary>
    public readonly bool IsFavoriteChallenge;
    public MiniGameRequestData(MiniGameType miniGameType, PartType partType, PartnerPartData part, bool isFavoriteChallenge = false)
    {
        MiniGameType = miniGameType;
        PartType = partType;
        Part = part;
        BasePrice = part != null ? part.Price : 0;
        IsFavoriteChallenge = isFavoriteChallenge;
    }
}
public sealed class MiniGameResultData
{
    public readonly PartType PartType;
    public readonly PartnerPartData ResultPart;
    public readonly long CostTime;
    /// <summary>交渉後に実際に支払う金額。</summary>
    public readonly int CostMoney;
    /// <summary>交渉前の定価。</summary>
    public readonly int BasePrice;
    /// <summary>獲得スコア。</summary>
    public readonly int Score;
    /// <summary>お気に入り商品への挑戦だったか。</summary>
    public readonly bool IsFavoriteChallenge;
    /// <summary>値引きできた額。</summary>
    public long DiscountAmount => BasePrice - CostMoney;
    /// <summary>定価に対して何割値引けたか。</summary>
    public float DiscountRate => BasePrice > 0 ? (float)DiscountAmount / BasePrice : 0f;
    public MiniGameResultData(PartType partType, PartnerPartData resultPart, long costTime, int costMoney, int basePrice, bool isFavoriteChallenge = false)
    {
        PartType = partType;
        ResultPart = resultPart;
        CostTime = costTime;
        CostMoney = costMoney;
        BasePrice = basePrice;
        IsFavoriteChallenge = isFavoriteChallenge;
    }
}
public class ShoppingModel : SingletonBase<ShoppingModel>
{
    /// <summary>品揃えのうち、お気に入り（目玉商品）にする部位の数。</summary>
    public const int FavoritePartCount = 1;
    /// <summary>お気に入り商品のグレード。失敗時に下げる余地が要るので最上位で出す。</summary>
    public const PartGrade FavoritePartGrade = PartGrade.Premium;
    /// <summary>お気に入りチャレンジの成功ライン。交渉バーのどこまで伸ばせば成功か（矢印の位置と揃える）。</summary>
    public const float FavoriteChallengeRequiredRate = 0.6f;
    /// <summary>成功に必要な値引き率（定価に対する割合）。</summary>
    public static float FavoriteChallengeRequiredDiscountRate =>
        MiniGameBase.MaxDiscountRate * FavoriteChallengeRequiredRate;

    private Dictionary<PartType, MiniGameResultData> _results = new Dictionary<PartType, MiniGameResultData>();
    /// <summary>部位ごとのミニゲーム結果。未実施の部位は null。</summary>
    public IReadOnlyDictionary<PartType, MiniGameResultData> Results => _results;
    // 買い物開始時に部位ごとに抽選した「その日の品揃え」
    private Dictionary<PartType, PartnerPartData> _offeredParts = new Dictionary<PartType, PartnerPartData>();
    public IReadOnlyDictionary<PartType, PartnerPartData> OfferedParts => _offeredParts;
    // 目玉商品としてチャレンジ対象になる部位
    private HashSet<PartType> _favoriteParts = new HashSet<PartType>();
    // チャレンジを実施済みの部位。一度挑んだら通常プレイに落ちる
    private HashSet<PartType> _challengedParts = new HashSet<PartType>();
    // チャレンジに成功して、お気に入りのまま買えた部位
    private HashSet<PartType> _achievedFavoriteParts = new HashSet<PartType>();
    /// <summary>その部位が今日の目玉商品か。</summary>
    public bool IsFavorite(PartType partType) => _favoriteParts.Contains(partType);
    /// <summary>お気に入りチャレンジに成功して、お気に入りのまま買えたか。</summary>
    public bool IsFavoriteAchieved(PartType partType) => _achievedFavoriteParts.Contains(partType);
    private GenderType _currentGender;
    private int _currentPartIndex = 0;
    private Subject<MiniGameRequestData> _startMiniGameSubject = new Subject<MiniGameRequestData>();
    public Observable<MiniGameRequestData> StartMiniGameObservable() => _startMiniGameSubject.AsObservable();
    private Subject<Unit> _shoppingFinished = new Subject<Unit>();
    public Observable<Unit> ShoppingFinishedObservable() => _shoppingFinished.AsObservable();
    public override void OnReset()
    {
        _results.Clear();
        _offeredParts.Clear();
        _favoriteParts.Clear();
        _challengedParts.Clear();
        _achievedFavoriteParts.Clear();
        _currentPartIndex = 0;
    }
    public void Init(GenderType gender)
    {
        _currentGender = gender;
    }

    public void StartShopping()
    {
        _currentPartIndex = 0;
        _results.Clear();
        _offeredParts.Clear();
        _favoriteParts.Clear();
        _challengedParts.Clear();
        _achievedFavoriteParts.Clear();

        var grades = (PartGrade[])System.Enum.GetValues(typeof(PartGrade));
        var partTypes = (PartType[])System.Enum.GetValues(typeof(PartType));
        foreach (var partType in partTypes)
        {
            _results[partType] = null;
            // 部位ごとにグレードを1つ抽選する。これがその日その店に並ぶ品になる
            var grade = grades[Random.Range(0, grades.Length)];
            _offeredParts[partType] = new PartnerPartData(partType, grade, _currentGender);
        }

        // 品揃えの中から決まった数だけ目玉商品を選び、最上位グレードで並べ直す
        var candidates = new List<PartType>(partTypes);
        var favoriteCount = Mathf.Clamp(FavoritePartCount, 0, candidates.Count);
        for (var i = 0; i < favoriteCount; i++)
        {
            var index = Random.Range(0, candidates.Count);
            var favoritePartType = candidates[index];
            candidates.RemoveAt(index);

            _favoriteParts.Add(favoritePartType);
            _offeredParts[favoritePartType] = new PartnerPartData(favoritePartType, FavoritePartGrade, _currentGender);
        }
    }

    public void StartMiniGame()
    {
        var partType = (PartType)_currentPartIndex;
        var miniGameType = Random.Range(0, 3) switch
        {
            0 => MiniGameType.Dogeza,
            1 => MiniGameType.Jyanken,
            2 => MiniGameType.TimeTap,
            _ => MiniGameType.Dogeza,
        };

        if (!_offeredParts.TryGetValue(partType, out var offeredPart) || offeredPart == null)
        {
            // StartShopping を経ずに来た場合の保険
            offeredPart = new PartnerPartData(partType, PartGrade.Budget, _currentGender);
            _offeredParts[partType] = offeredPart;
        }

        // 目玉商品はまず一発勝負のチャレンジ。落とすと通常プレイに降りる
        var isFavoriteChallenge = _favoriteParts.Contains(partType) && !_challengedParts.Contains(partType);

        var miniGameRequestData = new MiniGameRequestData(miniGameType, partType, offeredPart, isFavoriteChallenge);
        _startMiniGameSubject.OnNext(miniGameRequestData);
    }
    public void OnMiniGameFinished(MiniGameResultData miniGameResultData)
    {
        var partType = miniGameResultData.PartType;

        if (miniGameResultData.IsFavoriteChallenge)
        {
            _challengedParts.Add(partType);

            if (miniGameResultData.DiscountRate < FavoriteChallengeRequiredDiscountRate)
            {
                // 値切り切れなかったので、お気に入りは引っ込められる。
                // 一段下のグレードで並べ直して、同じ部位をもう一度やり直す
                DowngradeOfferedPart(partType);
                StartMiniGame();
                return;
            }

            _achievedFavoriteParts.Add(partType);
        }

        _results[miniGameResultData.PartType] = miniGameResultData;
        _currentPartIndex++;

        if (_currentPartIndex >= System.Enum.GetValues(typeof(PartType)).Length)
        {
            OnShoppingFinished();
        }
        else
        {
            StartMiniGame();
        }
    }
#if UNITY_EDITOR
    /// <summary>リザルト演出の確認用に、品揃えの抽選と結果をまとめて仮作成する。</summary>
    public void SetupDebugResults(GenderType gender)
    {
        Init(gender);
        StartShopping();

        foreach (PartType partType in System.Enum.GetValues(typeof(PartType)))
        {
            var part = _offeredParts[partType];
            var basePrice = part.Price;
            // 値引き交渉の結果を適当にばらけさせる
            var costMoney = Mathf.RoundToInt(basePrice * Random.Range(0.5f, 1f));
            _results[partType] = new MiniGameResultData(partType, part, Random.Range(1, 6), costMoney, basePrice);
            // 本来は買い物中に支払っているので、残り所持金にも反映しておく
            GameModel.Instance.OnMoneyPaid(costMoney);
        }

        _currentPartIndex = System.Enum.GetValues(typeof(PartType)).Length;
    }
#endif
    /// <summary>お気に入りチャレンジに失敗した部位を、一段下のグレードに差し替える。</summary>
    private void DowngradeOfferedPart(PartType partType)
    {
        if (!_offeredParts.TryGetValue(partType, out var current) || current == null) return;

        var lowestGrade = PartGrade.Budget;
        if (current.Grade >= lowestGrade) return; // これ以上下げられない

        var nextGrade = (PartGrade)((int)current.Grade + 1);
        _offeredParts[partType] = new PartnerPartData(partType, nextGrade, _currentGender);
    }

    public void OnShoppingFinished()
    {
        // All parts finished
        Debug.Log("All parts finished");
        foreach (var result in _results)
        {
            Debug.Log($"PartType: {result.Key}, Grade: {result.Value.ResultPart.Grade}, BasePrice: {result.Value.BasePrice}, CostMoney: {result.Value.CostMoney} (値引き {result.Value.DiscountAmount})");
        }
        _shoppingFinished.OnNext(Unit.Default);
    }
}
