using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using R3;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameKowtowRequestController : MiniGameBase
{
    [SerializeField] private Image _buttonBgImage;
    [SerializeField] private Button _clickButton;
    [SerializeField] private float _alphaDecreaseRate = 0.08f; // per second
    [SerializeField] private float _alphaThreshold = 0.2f;
    [SerializeField] private float _alphaRecoverAmount = 0.15f;

    [SerializeField] private RectTransform _barImageRootTransform;
    [SerializeField] private Image _barImage;
    [SerializeField] private float _fillDecreaseRate = 0.12f; // per second
    [SerializeField] private float _fillRecoverAmount = 0.2f;

    [Header("StaffReaction")]
    [SerializeField] private float _idleRequestInterval = 2.5f; // 無操作がこの秒数続いたら再度おねだりする
    [SerializeField] private float _reactionMinInterval = 0.8f; // リアクションのちらつき防止

    private const float MidGradeFillThreshold = 1f / 3f;
    private const float PremiumGradeFillThreshold = 2f / 3f;

    private CancellationTokenSource _cts;
    private CancellationTokenSource _barScaleCts;
    public override MiniGameType ThisMiniGameType => MiniGameType.Dogeza;
    private PartGrade _currentPartGrade;
    private float _lastReactionTime;

    private void Awake()
    {
        _clickButton.OnClickAsObservable()
            .Subscribe(_ => OnButtonClicked())
            .AddTo(this);
    }

    private void OnButtonClicked()
    {
        var color = _buttonBgImage.color;
        color.a = Mathf.Min(1f, color.a + _alphaRecoverAmount);
        _buttonBgImage.color = color;

        SetBarFillAmount(_barImage.fillAmount + _fillRecoverAmount);

        _barScaleCts?.Cancel();
        _barScaleCts?.Dispose();
        _barScaleCts = new CancellationTokenSource();
        PunchBarScaleAsync(_barScaleCts.Token).Forget();
    }
    [ContextMenu("Test Load Game")]
    public void TestLoadGame()
    {
        OnGameLoaded(new MiniGameRequestData(
            MiniGameType.Dogeza,
            PartType.Accessory,
            new PartnerPartData(PartType.Accessory, PartGrade.Budget, GenderType.Female)));
    }
    public override void OnGameLoaded(MiniGameRequestData requestData)
    {
        RequestData = requestData;
        gameObject.SetActive(true);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        ResetCostMoneyRoller();
    }
    public override void OnGameUnloaded()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _barScaleCts?.Cancel();
        _barScaleCts?.Dispose();
        _barScaleCts = null;
        gameObject.SetActive(false);
    }
    public override void OnGameStartRequested()
    {
        RunAlphaDecreaseAsync(_cts.Token).Forget();
        RunIdleReactionAsync(_cts.Token).Forget();
    }
    // public override void Init(UIShoppingCurrentPartStateView currentPartStateView)
    // {
    //     CurrentPartStateView = currentPartStateView;
    // }
    [ContextMenu("Test Finish Game")]
    public override void OnGameFinishRequested()
    {
        // 頭を下げ続けて溜めたバーがそのまま交渉の成果になる
        CurrentMiniGameResult = CreateResult(_barImage.fillAmount);
        UpdateCostMoneyRoller(_barImage.fillAmount, force: true);

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _barScaleCts?.Cancel();
        _barScaleCts?.Dispose();
        _barScaleCts = null;
        gameObject.SetActive(false);
        SetBarFillAmount(0, false);
        // CurrentPartStateView = null;
    }

    private async UniTaskVoid PunchBarScaleAsync(CancellationToken ct)
    {
        const float punchScale = 1.06f;
        const float duration = 0.2f;
        _barImageRootTransform.localScale = Vector3.one * punchScale;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var scale = Mathf.Lerp(punchScale, 1f, elapsed / duration);
            _barImageRootTransform.localScale = Vector3.one * scale;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        _barImageRootTransform.localScale = Vector3.one;
    }

    private async UniTaskVoid RunAlphaDecreaseAsync(CancellationToken ct)
    {
        var color = _buttonBgImage.color;
        color.a = 1f;
        _buttonBgImage.color = color;
        SetBarFillAmount(0f, notifyReaction: false);

        while (!ct.IsCancellationRequested)
        {
            color = _buttonBgImage.color;
            color.a = Mathf.Max(_alphaThreshold, color.a - _alphaDecreaseRate * Time.deltaTime);
            _buttonBgImage.color = color;

            SetBarFillAmount(_barImage.fillAmount - _fillDecreaseRate * Time.deltaTime);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    // 一定時間リアクションが出ていなければ、スタッフが改めておねだりする
    private async UniTaskVoid RunIdleReactionAsync(CancellationToken ct)
    {
        EmitStaffReaction(StaffReactionCode.Dogeza_Request, ignoreMinInterval: true);

        while (!ct.IsCancellationRequested)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, ct);

            if (Time.time - _lastReactionTime >= _idleRequestInterval)
            {
                EmitStaffReaction(StaffReactionCode.Dogeza_Request, ignoreMinInterval: true);
            }
        }
    }

    private void SetBarFillAmount(float fillAmount, bool notifyReaction = true)
    {
        var clampedFillAmount = Mathf.Clamp01(fillAmount);
        _barImage.fillAmount = clampedFillAmount;
        UpdateCostMoneyRoller(clampedFillAmount);
        var nextGrade = clampedFillAmount >= PremiumGradeFillThreshold
            ? PartGrade.Premium
            : clampedFillAmount >= MidGradeFillThreshold
                ? PartGrade.Mid
                : PartGrade.Budget;

        if (notifyReaction && nextGrade != _currentPartGrade)
        {
            // PartGrade は Premium=0 が最上位なので、値が小さくなるほどグレードアップ
            if (nextGrade < _currentPartGrade)
            {
                EmitStaffReaction(nextGrade == PartGrade.Premium
                    ? StaffReactionCode.Dogeza_Happy
                    : StaffReactionCode.Dogeza_Surprised);
            }
            else
            {
                EmitStaffReaction(StaffReactionCode.Dogeza_Disappointed);
            }
        }

        _currentPartGrade = nextGrade;
    }

    private void EmitStaffReaction(StaffReactionCode code, bool ignoreMinInterval = false)
    {
        if (!ignoreMinInterval && Time.time - _lastReactionTime < _reactionMinInterval) return;

        _lastReactionTime = Time.time;
        RequestStaffReaction(code);
    }
}
