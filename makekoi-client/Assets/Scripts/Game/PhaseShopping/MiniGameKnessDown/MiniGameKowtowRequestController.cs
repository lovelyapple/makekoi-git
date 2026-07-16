using Cysharp.Threading.Tasks;
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

    private CancellationTokenSource _cts;
    private CancellationTokenSource _barScaleCts;
    public override MiniGameType ThisMiniGameType => MiniGameType.Dogeza;

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

        _barImage.fillAmount = Mathf.Min(1f, _barImage.fillAmount + _fillRecoverAmount);

        _barScaleCts?.Cancel();
        _barScaleCts?.Dispose();
        _barScaleCts = new CancellationTokenSource();
        PunchBarScaleAsync(_barScaleCts.Token).Forget();
    }
    [ContextMenu("Test Load Game")]
    public override void OnGameLoaded()
    {
        gameObject.SetActive(true);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
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
    }
    public override void Init(UIShoppingCurrentPartStateView currentPartStateView)
    {
        CurrentPartStateView = currentPartStateView;
    }
    [ContextMenu("Test Finish Game")]
    public override void OnGameFinishRequested()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _barScaleCts?.Cancel();
        _barScaleCts?.Dispose();
        _barScaleCts = null;
        CurrentPartStateView = null;
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
        _barImage.fillAmount = 0f;

        while (!ct.IsCancellationRequested)
        {
            color = _buttonBgImage.color;
            color.a = Mathf.Max(_alphaThreshold, color.a - _alphaDecreaseRate * Time.deltaTime);
            _buttonBgImage.color = color;

            _barImage.fillAmount = Mathf.Max(0f, _barImage.fillAmount - _fillDecreaseRate * Time.deltaTime);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }
}
