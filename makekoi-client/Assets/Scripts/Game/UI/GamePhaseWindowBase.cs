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
    /// <summary>順路を飛ばして任意のフェーズへ戻す。買い物失敗でタイトルへ戻すときなど。</summary>
    public void RequestGoToPhase(GamePhaseContentType phase)
    {
        _onGotoNextPhaseSubject.OnNext(phase);
    }
    /// <summary>
    /// フェーズに入るときの入口。BGM を切り替えてから OnEnter を呼ぶ。
    /// OnEnter を直接呼ぶと BGM が切り替わらないので、表示側はこちらを使うこと。
    /// </summary>
    public void EnterPhase()
    {
        PlayPhaseBgm();
        OnEnter();
    }
    /// <summary>買い物までは通しで同じ曲、買い物と最終結果でそれぞれ切り替える。</summary>
    private static string GetPhaseBgmType(GamePhaseContentType phase)
    {
        switch (phase)
        {
            case GamePhaseContentType.Title:
            case GamePhaseContentType.CharaSelect:
                return CommonBgmType.PhaseMain;
            case GamePhaseContentType.Shopping:
                return CommonBgmType.PhaseShopping;
            case GamePhaseContentType.Result:
                return CommonBgmType.PhaseResult;
            default:
                throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
        }
    }
    private void PlayPhaseBgm()
    {
        // 同じ曲のままのフェーズ移動は BgmFader 側で弾かれるので、頭から鳴り直さない
        SoundManager.PlayBgm(SoundManager.GetBgmByType(GetPhaseBgmType(ThisPhaseContentType)));
    }
    public virtual void OnEnter()
    { }
}
