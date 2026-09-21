using System.Threading;
using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameTimingTapController : MiniGameBase
{
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

    private CancellationTokenSource _cts;
    private float _rangeLeft;
    private float _rangeRight;
    private PartGrade _currentPartGrade;
    private bool _hasTapped;
    private int _correctTapCount;
    private bool _isFinishing;
    private bool _isPenalty;
    private float _lastReactionTime;

    public override MiniGameType ThisMiniGameType => MiniGameType.TimeTap;

    private void Awake()
    {
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
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
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
        var distanceFromRange = cursorValue < _rangeLeft
            ? _rangeLeft - cursorValue
            : cursorValue > _rangeRight
                ? cursorValue - _rangeRight
                : 0f;

        if (distanceFromRange <= 0f)
        {
            // 範囲内 = 値引き交渉成立
            EmitStaffReaction(StaffReactionCode.TimeTap_Success);
            _correctTapCount++;
            _cursorMoveSpeed = Mathf.Min(
                _maxCursorMoveSpeed,
                _cursorMoveSpeed + _speedIncreasePerCorrectTap);
            UpdatePartGrade();

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
        SetupRange(left, left + width);
    }

    private void SetupRange(float leftRange, float rightRange)
    {
        _rangeLeft = leftRange;
        _rangeRight = rightRange;
        _leftBgRangeImage.fillAmount = leftRange;
        _rightBgRangeImage.fillAmount = 1f - rightRange;
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

    private void StopGame()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
