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

    /// <summary>交渉で下げられる値引きの上限 (定価に対する割合)。</summary>
    public const float MaxDiscountRate = 0.5f;

    /// <summary>
    /// 交渉の成果 (0=値引きなし〜1=満額の値引き) から結果を作る。
    /// パーツは買い物開始時に抽選済みなので、ミニゲームの出来は値段だけに効く。
    /// </summary>
    protected MiniGameResultData CreateResult(float negotiationRate)
    {
        var rate = Mathf.Clamp01(negotiationRate);
        var basePrice = RequestData.BasePrice;
        var discount = (long)Mathf.Round(basePrice * MaxDiscountRate * rate);
        return new MiniGameResultData(RequestData.PartType, RequestData.Part, 0, basePrice - discount, basePrice);
    }

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
