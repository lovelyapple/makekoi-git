using Cysharp.Threading.Tasks;
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
    [SerializeField] protected UINumberRollerGroup _numberRollerGroup;
    public abstract MiniGameType ThisMiniGameType { get; }
    public MiniGameRequestData RequestData { get; protected set; }
    public MiniGameResultData CurrentMiniGameResult { get; protected set; }

    private readonly Subject<StaffReactionCode> _staffReactionRequested = new Subject<StaffReactionCode>();
    public Observable<StaffReactionCode> StaffReactionRequestedObservable() => _staffReactionRequested.AsObservable();
    protected void RequestStaffReaction(StaffReactionCode code) => _staffReactionRequested.OnNext(code);

    /// <summary>交渉で下げられる値引きの上限 (定価に対する割合)。</summary>
    public const float MaxDiscountRate = 0.5f;

    /// <summary>ドラムを回し直す最短間隔。プレイ中に値が毎フレーム動いても回り続けないようにする。</summary>
    [SerializeField] private float _costMoneyRollInterval = 0.2f;

    private long _rolledCostMoney = -1;
    private float _lastCostMoneyRollTime;

    /// <summary>
    /// 交渉の成果 (0=値引きなし〜1=満額の値引き) から、実際に支払う金額を求める。
    /// </summary>
    protected long CalcCostMoney(float negotiationRate)
    {
        var rate = Mathf.Clamp01(negotiationRate);
        var basePrice = RequestData.BasePrice;
        var discount = (long)Mathf.Round(basePrice * MaxDiscountRate * rate);
        return basePrice - discount;
    }

    /// <summary>
    /// 交渉の成果 (0=値引きなし〜1=満額の値引き) から結果を作る。
    /// パーツは買い物開始時に抽選済みなので、ミニゲームの出来は値段だけに効く。
    /// </summary>
    protected MiniGameResultData CreateResult(float negotiationRate)
    {
        return new MiniGameResultData(
            RequestData.PartType, RequestData.Part, 0, (int)CalcCostMoney(negotiationRate), (int)RequestData.BasePrice);
    }

    /// <summary>値段のドラムを定価に戻す。ゲーム開始時に呼ぶ。</summary>
    protected void ResetCostMoneyRoller()
    {
        if (_numberRollerGroup == null || RequestData == null) return;

        _rolledCostMoney = RequestData.BasePrice;
        _lastCostMoneyRollTime = 0f;
        _numberRollerGroup.SetValueImmediate((int)RequestData.BasePrice);
    }

    /// <summary>
    /// 今の交渉成果を値段のドラムに反映する。毎フレーム呼んでよい。
    /// 金額が変わっていないときと、前に回してから間隔が空いていないときは何もしない。
    /// </summary>
    /// <param name="force">間隔を無視して必ず反映する。金額が確定したときに使う。</param>
    protected void UpdateCostMoneyRoller(float negotiationRate, bool force = false)
    {
        if (_numberRollerGroup == null || RequestData == null) return;

        var costMoney = CalcCostMoney(negotiationRate);
        if (costMoney == _rolledCostMoney) return;
        if (!force && Time.time - _lastCostMoneyRollTime < _costMoneyRollInterval) return;

        _rolledCostMoney = costMoney;
        _lastCostMoneyRollTime = Time.time;
        _numberRollerGroup.FollowToAsync((int)costMoney).Forget();
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
