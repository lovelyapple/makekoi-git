using R3;
using UnityEngine;
using System;
public class GamePhaseWindowBase : MonoBehaviour
{
    public static GamePhaseContentType GetNextPhaseContentType(GamePhaseContentType currentPhase)
    {
        switch (currentPhase)
        {
            case GamePhaseContentType.Title:
                return GamePhaseContentType.CharaSelect;
            case GamePhaseContentType.CharaSelect:
                return GamePhaseContentType.Shopping;
            case GamePhaseContentType.Shopping:
                return GamePhaseContentType.Result;
            case GamePhaseContentType.Result:
                return GamePhaseContentType.Title;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentPhase), currentPhase, null);
        }
    }
    public virtual GamePhaseContentType ThisPhaseContentType => GamePhaseContentType.Title;
    private Subject<GamePhaseContentType> _onGotoNextPhaseSubject = new Subject<GamePhaseContentType>();
    public Observable<GamePhaseContentType> OnRequestGoToNextPhase() => _onGotoNextPhaseSubject.AsObservable();
    public void RequestGoToNextPhase()
    {
        var nextPhase = GetNextPhaseContentType(ThisPhaseContentType);
        _onGotoNextPhaseSubject.OnNext(nextPhase);
    }
    public virtual void OnEnter()
    { }
}
