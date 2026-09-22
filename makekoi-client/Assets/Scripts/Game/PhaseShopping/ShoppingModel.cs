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
    public MiniGameRequestData(MiniGameType miniGameType, PartType partType, PartnerPartData part)
    {
        MiniGameType = miniGameType;
        PartType = partType;
        Part = part;
        BasePrice = part != null ? part.Price : 0;
    }
}
public sealed class MiniGameResultData
{
    public readonly PartType PartType;
    public readonly PartnerPartData ResultPart;
    public readonly long CostTime;
    /// <summary>交渉後に実際に支払う金額。</summary>
    public readonly long CostMoney;
    /// <summary>交渉前の定価。</summary>
    public readonly long BasePrice;
    /// <summary>値引きできた額。</summary>
    public long DiscountAmount => BasePrice - CostMoney;
    public MiniGameResultData(PartType partType, PartnerPartData resultPart, long costTime, long costMoney, long basePrice)
    {
        PartType = partType;
        ResultPart = resultPart;
        CostTime = costTime;
        CostMoney = costMoney;
        BasePrice = basePrice;
    }
}
public class ShoppingModel : SingletonBase<ShoppingModel>
{
    private Dictionary<PartType, MiniGameResultData> _results = new Dictionary<PartType, MiniGameResultData>();
    // 買い物開始時に部位ごとに抽選した「その日の品揃え」
    private Dictionary<PartType, PartnerPartData> _offeredParts = new Dictionary<PartType, PartnerPartData>();
    public IReadOnlyDictionary<PartType, PartnerPartData> OfferedParts => _offeredParts;
    private GenderType _currentGender;
    private int _currentPartIndex = 0;
    private Subject<MiniGameRequestData> _startMiniGameSubject = new Subject<MiniGameRequestData>();
    public Observable<MiniGameRequestData> StartMiniGameObservable() => _startMiniGameSubject.AsObservable();
    private Subject<Unit> _shoppingFinished = new Subject<Unit>();
    public Observable<Unit> ShoppingFinishedObservable() => _shoppingFinished.AsObservable();
    public void Init(GenderType gender)
    {
        _currentGender = gender;
    }

    public void StartShopping()
    {
        _currentPartIndex = 0;
        _results.Clear();
        _offeredParts.Clear();

        var grades = (PartGrade[])System.Enum.GetValues(typeof(PartGrade));
        foreach (PartType partType in System.Enum.GetValues(typeof(PartType)))
        {
            _results[partType] = null;
            // 部位ごとにグレードを1つ抽選する。これがその日その店に並ぶ品になる
            var grade = grades[Random.Range(0, grades.Length)];
            _offeredParts[partType] = new PartnerPartData(partType, grade, _currentGender);
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

        var miniGameRequestData = new MiniGameRequestData(miniGameType, partType, offeredPart);
        _startMiniGameSubject.OnNext(miniGameRequestData);
    }
    public void OnMiniGameFinished(MiniGameResultData miniGameResultData)
    {
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
