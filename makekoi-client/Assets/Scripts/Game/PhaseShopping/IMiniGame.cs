using Makekoi.PartnerCreate;
using R3;
using UnityEngine;

public interface IMiniGame
{
    public MiniGameType ThisMiniGameType { get; }
    public MiniGameRequestData RequestData { get; }
    public MiniGameResultData CurrentMiniGameResult { get; }
    // ミニゲーム進行中にスタッフへ出させたいリアクションを通知する
    public Observable<StaffReactionCode> StaffReactionRequestedObservable();
    // public UIShoppingCurrentPartStateView CurrentPartStateView { get; set; }
    // public void Init(UIShoppingCurrentPartStateView currentPartStateView);
    public void OnGameLoaded(MiniGameRequestData requestData);
    public void OnGameUnloaded();
    public void OnGameStartRequested();
    public void OnGameFinishRequested();
}
public abstract class MiniGameBase : MonoBehaviour, IMiniGame
{
    public abstract MiniGameType ThisMiniGameType { get; }
    public MiniGameRequestData RequestData { get; protected set; }
    public MiniGameResultData CurrentMiniGameResult { get; protected set; }

    private readonly Subject<StaffReactionCode> _staffReactionRequested = new Subject<StaffReactionCode>();
    public Observable<StaffReactionCode> StaffReactionRequestedObservable() => _staffReactionRequested.AsObservable();
    protected void RequestStaffReaction(StaffReactionCode code) => _staffReactionRequested.OnNext(code);

    // public UIShoppingCurrentPartStateView CurrentPartStateView { get; set; }
    // public virtual void Init(UIShoppingCurrentPartStateView currentPartStateView) { }
    public virtual void OnGameLoaded(MiniGameRequestData requestData)
    {
        RequestData = requestData;
    }
    public virtual void OnGameUnloaded() { }
    public virtual void OnGameStartRequested() { }
    public virtual void OnGameFinishRequested() { }

    protected virtual void OnDestroy()
    {
        _staffReactionRequested.Dispose();
    }
}
