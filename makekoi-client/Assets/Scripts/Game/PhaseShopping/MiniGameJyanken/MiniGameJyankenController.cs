using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameJyankenController : MiniGameBase
{
    [SerializeField] private Button _rockButton;
    [SerializeField] private Button _paperButton;
    [SerializeField] private Button _scissorsButton;

    [SerializeField] private Image _refRock;
    [SerializeField] private Image _refPaper;
    [SerializeField] private Image _refScissors;

    [SerializeField] private Image _barImage;

    [Header("Button Points")]
    [SerializeField] private RectTransform _pointLeft;
    [SerializeField] private RectTransform _pointCenter;
    [SerializeField] private RectTransform _pointRight;

    [SerializeField] private Image _winFrame;
    [SerializeField] private Image _loseFrame;

    private enum Hand
    {
        Rock,
        Paper,
        Scissors,
    }

    private const int MaxBarValue = 5;

    private readonly List<Button> _buttons = new();
    private CancellationTokenSource _cts;
    private CancellationTokenSource _resultFrameCts;
    private Hand _referenceHand;
    private int _barValue;
    private bool _isGameActive;

    public override MiniGameType ThisMiniGameType => MiniGameType.Jyanken;

    private void Awake()
    {
        _buttons.Add(_rockButton);
        _buttons.Add(_paperButton);
        _buttons.Add(_scissorsButton);

        _rockButton.OnClickAsObservable().Subscribe(_ => OnPlayerSelected(Hand.Rock)).AddTo(this);
        _paperButton.OnClickAsObservable().Subscribe(_ => OnPlayerSelected(Hand.Paper)).AddTo(this);
        _scissorsButton.OnClickAsObservable().Subscribe(_ => OnPlayerSelected(Hand.Scissors)).AddTo(this);
    }

    public override void OnGameLoaded(MiniGameRequestData requestData)
    {
        RequestData = requestData;
        CurrentMiniGameResult = null;
        _barValue = 0;
        _isGameActive = false;
        gameObject.SetActive(true);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        SetAllButtonsPosition(_pointCenter);
        SetButtonsInteractable(false);
        ResetCostMoneyRoller();
        SetBarValue(_barValue);
        HideReferences();
    }

    public override void OnGameStartRequested()
    {
        _isGameActive = true;
        SetButtonsInteractable(true);
        SelectReferenceHand();
    }

    public override void OnGameFinishRequested()
    {
        _isGameActive = false;
        SetButtonsInteractable(false);

        // 勝ち越した分がそのまま交渉の成果になる
        CurrentMiniGameResult = CreateResult((float)_barValue / MaxBarValue);
        UpdateCostMoneyRoller((float)_barValue / MaxBarValue, force: true);

        StopGame();
    }

    public override void OnGameUnloaded()
    {
        _isGameActive = false;
        SetButtonsInteractable(false);
        StopGame();
        gameObject.SetActive(false);
    }

    private void OnPlayerSelected(Hand playerHand)
    {
        if (!_isGameActive) return;

        var result = Judge(playerHand, _referenceHand);
        if (result == 1)
        {
            SetBarValue(_barValue + 1);
            PlayResultFrame(true);
        }
        else if (result == -1)
        {
            SetBarValue(_barValue - 1);
            PlayResultFrame(false);
        }

        SetButtonsInteractable(false);
        SetAllButtonsPosition(_pointCenter);
        SelectReferenceHand();
    }

    private void SelectReferenceHand()
    {
        _referenceHand = (Hand)UnityEngine.Random.Range(0, 3);
        _refRock.gameObject.SetActive(_referenceHand == Hand.Rock);
        _refPaper.gameObject.SetActive(_referenceHand == Hand.Paper);
        _refScissors.gameObject.SetActive(_referenceHand == Hand.Scissors);

        SetButtonsToRandomPoints();
        SetButtonsInteractable(_isGameActive);
    }

    private void SetButtonsToRandomPoints()
    {
        var points = new List<RectTransform>
        {
            _pointLeft,
            _pointCenter,
            _pointRight,
        };

        for (var index = points.Count - 1; index > 0; index--)
        {
            var swapIndex = UnityEngine.Random.Range(0, index + 1);
            (points[index], points[swapIndex]) = (points[swapIndex], points[index]);
        }

        for (var index = 0; index < _buttons.Count; index++)
        {
            SetButtonPosition(_buttons[index], points[index]);
        }
    }

    private void SetAllButtonsPosition(RectTransform point)
    {
        if (point == null) return;

        foreach (var button in _buttons)
        {
            SetButtonPosition(button, point);
        }
    }

    private static void SetButtonPosition(Button button, RectTransform point)
    {
        var buttonTransform = button.transform as RectTransform;
        buttonTransform.anchoredPosition = point.anchoredPosition;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (var button in _buttons)
        {
            button.interactable = interactable;
        }
    }

    private void HideReferences()
    {
        _refRock.gameObject.SetActive(false);
        _refPaper.gameObject.SetActive(false);
        _refScissors.gameObject.SetActive(false);
    }

    private void SetBarValue(int value)
    {
        _barValue = Mathf.Clamp(value, 0, MaxBarValue);
        _barImage.fillAmount = (float)_barValue / MaxBarValue;
        UpdateCostMoneyRoller(_barImage.fillAmount);
    }

    private static int Judge(Hand playerHand, Hand referenceHand)
    {
        if (playerHand == referenceHand) return 0;
        return (playerHand == Hand.Rock && referenceHand == Hand.Scissors)
            || (playerHand == Hand.Paper && referenceHand == Hand.Rock)
            || (playerHand == Hand.Scissors && referenceHand == Hand.Paper)
            ? 1
            : -1;
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
        // 勝ち負けが決まった瞬間に鳴らす。引き分けはここまで来ない
        SoundManager.PlaySeByType(isWin
            ? CommonSeType.mini_game_action_win
            : CommonSeType.mini_game_action_lose);

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
            var pingPong = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
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
