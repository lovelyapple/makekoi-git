using System.Threading;
using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameTimingTapController : MiniGameBase
{
    [SerializeField] private Image _barImage;
    [SerializeField] private Button _clickButton;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private Image _leftBgRangeImage;
    [SerializeField] private Image _rightBgRangeImage;
    [SerializeField] private float _cursorMoveSpeed = 0.8f;
    [SerializeField] private float _minRangeWidth = 0.2f;
    [SerializeField] private float _maxRangeWidth = 0.35f;
    [SerializeField] private float _nearMissDistance = 0.1f;
    [SerializeField] private float _speedIncreasePerCorrectTap = 0.3f;
    [SerializeField] private float _maxCursorMoveSpeed = 2f;
    [SerializeField] private int _maxCorrectTapCount = 5;
    [SerializeField] private float _failurePenaltyDuration = 1f; // 交渉を断られた後、次にタップできるまでの時間

    [Header("StaffReaction")]
    [SerializeField] private float _reactionMinInterval = 0.8f; // リアクションのちらつき防止
    [SerializeField] private Image _winFrame;
    [SerializeField] private Image _loseFrame;

    private CancellationTokenSource _cts;
    private CancellationTokenSource _resultFrameCts;
    private float _rangeLeft;
    private float _rangeRight;
    private float _initialCursorMoveSpeed;
    private PartGrade _currentPartGrade;
    private bool _hasTapped;
    private int _correctTapCount;
    private bool _isFinishing;
    private bool _isPenalty;
    private float _lastReactionTime;

    public override MiniGameType ThisMiniGameType => MiniGameType.TimeTap;

    private void Awake()
    {
        _initialCursorMoveSpeed = _cursorMoveSpeed;
        SetBarFillAmount(0);
        _clickButton.OnClickAsObservable()
            .Subscribe(_ => OnButtonClicked())
            .AddTo(this);
    }

    public override void OnGameLoaded(MiniGameRequestData requestData)
    {
        RequestData = requestData;
        CurrentMiniGameResult = null;
        _hasTapped = false;
        _correctTapCount = 0;
        _isFinishing = false;
        _isPenalty = false;
        _currentPartGrade = PartGrade.Budget;
        gameObject.SetActive(true);
        SetBarFillAmount(0);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _cursorMoveSpeed = _initialCursorMoveSpeed;
        ResetRange();

        // 交渉が始まるまでの待ちの一言
        EmitStaffReaction(StaffReactionCode.TimeTap_Idle, ignoreMinInterval: true);
    }

    public override void OnGameUnloaded()
    {
        StopGame();
        gameObject.SetActive(false);
    }

    public override void OnGameStartRequested()
    {
        _hasTapped = false;
        _correctTapCount = 0;
        _isFinishing = false;
        _isPenalty = false;
        _clickButton.interactable = true;
        SetBarFillAmount(0);
        _progressSlider.normalizedValue = 0f;
        _progressSlider.transform.localScale = Vector3.one;
        EmitStaffReaction(StaffReactionCode.TimeTap_Request, ignoreMinInterval: true);
        RunCursorAsync(_cts.Token).Forget();
    }

    public override void OnGameFinishRequested()
    {
        var partData = RequestData.Parts.Find(part => part.Grade == _currentPartGrade);
        if (partData != null)
        {
            CurrentMiniGameResult = new MiniGameResultData(RequestData.PartType, partData, 0, 0);
        }

        StopGame();
    }

    private void OnButtonClicked()
    {
        if (_hasTapped || _isFinishing || _isPenalty) return;

        var cursorValue = _progressSlider.normalizedValue;
        var inRange = cursorValue >= _rangeLeft && cursorValue <= _rangeRight;

        Debug.Log($"Cursor Value: {cursorValue}, In Range: {inRange}, Range Left: {_rangeLeft}, Range Right: {_rangeRight}");

        if (inRange)
        {
            // 範囲内 = 値引き交渉成立
            EmitStaffReaction(StaffReactionCode.TimeTap_Success);
            PlayResultFrame(true);
            _correctTapCount++;
            _cursorMoveSpeed = Mathf.Min(
                _maxCursorMoveSpeed,
                _cursorMoveSpeed + _speedIncreasePerCorrectTap);
            UpdatePartGrade();
            SetBarFillAmount(_correctTapCount);
            ResetRange();

            if (_correctTapCount >= _maxCorrectTapCount)
            {
                _hasTapped = true;
                _isFinishing = true;
                _clickButton.interactable = false;
                PlayFinishEffectAsync().Forget();
            }
        }
        else
        {
            // 範囲外 = 値引きを断られる。しばらくタップできなくなる
            EmitStaffReaction(StaffReactionCode.TimeTap_Failure);
            PlayResultFrame(false);
            _correctTapCount = Mathf.Max(0, _correctTapCount - 1);
            UpdatePartGrade();
            SetBarFillAmount(_correctTapCount);
            _cursorMoveSpeed = _initialCursorMoveSpeed;
            ApplyFailurePenaltyAsync(_cts.Token).Forget();
        }
    }

    private async UniTaskVoid ApplyFailurePenaltyAsync(CancellationToken ct)
    {
        _isPenalty = true;
        _clickButton.interactable = false;

        var isCanceled = await UniTask
            .Delay(System.TimeSpan.FromSeconds(_failurePenaltyDuration), cancellationToken: ct)
            .SuppressCancellationThrow();
        if (isCanceled) return;

        ResetRange();
        _isPenalty = false;
        if (!_isFinishing && !_hasTapped)
        {
            _clickButton.interactable = true;
        }
    }

    private void EmitStaffReaction(StaffReactionCode code, bool ignoreMinInterval = false)
    {
        if (!ignoreMinInterval && Time.time - _lastReactionTime < _reactionMinInterval) return;

        _lastReactionTime = Time.time;
        RequestStaffReaction(code);
    }

    private void ResetRange()
    {
        var width = Random.Range(_minRangeWidth, _maxRangeWidth);
        var left = Random.Range(0f, 1f - width);
        SetupRange(left, width);
    }

    private void SetupRange(float leftRange, float width)
    {
        _leftBgRangeImage.fillAmount = leftRange;
        _rightBgRangeImage.fillAmount = leftRange + width;

        _rangeLeft = leftRange;
        _rangeRight = leftRange + width;
    }

    private async UniTaskVoid RunCursorAsync(CancellationToken ct)
    {
        var direction = 1f;
        while (!ct.IsCancellationRequested && !_isFinishing)
        {
            var nextValue = _progressSlider.normalizedValue + direction * _cursorMoveSpeed * Time.deltaTime;
            if (nextValue >= 1f)
            {
                nextValue = 1f;
                direction = -1f;
            }
            else if (nextValue <= 0f)
            {
                nextValue = 0f;
                direction = 1f;
            }

            _progressSlider.normalizedValue = nextValue;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    private async UniTaskVoid PlayFinishEffectAsync()
    {
        const float duration = 0.5f;
        const float finishScale = 1.15f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var normalizedTime = Mathf.Clamp01(elapsed / duration);
            var scale = Mathf.Lerp(1f, finishScale, Mathf.Sin(normalizedTime * Mathf.PI));
            _progressSlider.transform.localScale = Vector3.one * scale;
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
        }

        _progressSlider.transform.localScale = Vector3.one;
    }

    private void UpdatePartGrade()
    {
        _currentPartGrade = _correctTapCount >= _maxCorrectTapCount
            ? PartGrade.Premium
            : _correctTapCount >= 4
                ? PartGrade.Mid
                : PartGrade.Budget;
    }

    private void SetBarFillAmount(int correctTapCount)
    {
        var maxCorrectTapCount = Mathf.Max(1, _maxCorrectTapCount);
        var clampedCorrectTapCount = Mathf.Clamp(correctTapCount, 0, maxCorrectTapCount);
        _barImage.fillAmount = (float)clampedCorrectTapCount / maxCorrectTapCount;
    }

    private void StopGame()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _resultFrameCts?.Cancel();
        _resultFrameCts?.Dispose();
        _resultFrameCts = null;
        gameObject.SetActive(false);
    }

    private void PlayResultFrame(bool isWin)
    {
        _resultFrameCts?.Cancel();
        _resultFrameCts?.Dispose();
        _resultFrameCts = new CancellationTokenSource();

        PlayResultFrameAsync(isWin, _resultFrameCts.Token).Forget();
    }

    private async UniTaskVoid PlayResultFrameAsync(bool isWin, CancellationToken ct)
    {
        var targetFrame = isWin ? _winFrame : _loseFrame;
        var otherFrame = isWin ? _loseFrame : _winFrame;
        otherFrame.color = SetAlpha(otherFrame.color, 0f);
        targetFrame.color = SetAlpha(targetFrame.color, 0f);
        targetFrame.gameObject.SetActive(true);

        const float duration = 0.5f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var normalizedTime = Mathf.Clamp01(elapsed / duration);
            var pingPong = Mathf.Sin(normalizedTime * Mathf.PI);
            targetFrame.color = SetAlpha(targetFrame.color, pingPong);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        targetFrame.color = SetAlpha(targetFrame.color, 0f);
        targetFrame.gameObject.SetActive(false);
    }

    private static Color SetAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
