using System.Collections.Generic;
using Makekoi.PartnerCreate;
using R3;
using UnityEngine;
using UnityEngine.Analytics;
public enum MiniGameType
{
    Dogeza,
    Quiz,
    TimeTap,
}
public sealed class MiniGameRequestData
{
    public readonly MiniGameType MiniGameType;
    public readonly PartType PartType;
    public readonly List<PartnerPartData> Parts;
    public MiniGameRequestData(MiniGameType miniGameType, PartType partType, List<PartnerPartData> parts)
    {
        MiniGameType = miniGameType;
        PartType = partType;
        Parts = parts;
    }

}
public sealed class MiniGameResultData
{
    public readonly PartType PartType;
    public readonly List<PartnerPartData> Parts;
    public readonly long CostTime;
    public readonly long CostMoney;
    public MiniGameResultData(PartType partType, List<PartnerPartData> parts, long costTime, long costMoney)
    {
        PartType = partType;
        Parts = parts;
        CostTime = costTime;
        CostMoney = costMoney;
    }
}
public class ShoppingModel : SingletonBase<ShoppingModel>
{
    private Dictionary<PartType, MiniGameResultData> _results = new Dictionary<PartType, MiniGameResultData>();
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
        foreach (PartType partType in System.Enum.GetValues(typeof(PartType)))
        {
            _results.Add(partType, null);
        }
    }

    public void StartMiniGame()
    {
        var partType = (PartType)_currentPartIndex;
        var miniGameType = Random.Range(0, 3) switch
        {
            0 => MiniGameType.Dogeza,
            1 => MiniGameType.Quiz,
            2 => MiniGameType.TimeTap,
            _ => MiniGameType.Dogeza,
        };

        var parts = new List<PartnerPartData>();
        foreach (PartGrade grade in System.Enum.GetValues(typeof(PartGrade)))
        {
            var partData = new PartnerPartData(partType, (PartGrade)grade, _currentGender);
            parts.Add(partData);
        }
        var miniGameRequestData = new MiniGameRequestData(miniGameType, partType, parts);
        _startMiniGameSubject.OnNext(miniGameRequestData);
    }
    public void OnMiniGameFinished(MiniGameResultData miniGameResultData)
    {
        _results[miniGameResultData.PartType] = miniGameResultData;
        _currentPartIndex++;

        if (_currentPartIndex >= System.Enum.GetValues(typeof(PartType)).Length)
        {
            // All parts finished
            Debug.Log("All parts finished");
            foreach (var result in _results)
            {
                Debug.Log($"PartType: {result.Key}, CostTime: {result.Value.CostTime}, CostMoney: {result.Value.CostMoney}");
            }
        }

        _shoppingFinished.OnNext(Unit.Default);
    }
}
