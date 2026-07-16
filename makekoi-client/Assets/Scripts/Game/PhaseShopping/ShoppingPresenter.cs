using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ShoppingPresenter : GamePhaseWindowBase
{
    [SerializeField] private UIMiniGameCharaReactionView _charaReactionView;
    [SerializeField] private UIShoppingCurrentPartStateView _currentPartStateView;
    [SerializeField] private List<IMiniGame> _miniGameControllers;
    [SerializeField] private Button _welcomeReactionButton;
    [SerializeField] private GameObject _miniGameRoot;
    #region MiniGame_phase
    private const float MiniGamePhaseDuration = 5f;
    private float _currentMiniGamePhaseTimeLeft = 0f;
    private PartType _currentPartType;
    private PartGrade _currentPartGrade;
    private IMiniGame _currentGameController;
    #endregion
    CancellationTokenSource _miniGameStartPerformanceCts;
    private void Awake()
    {
        ShoppingModel.Instance.StartMiniGameObservable()
            .Subscribe(OnStartMiniGameRequested)
            .AddTo(this);


        DebugModel.Instance.OnFinishCurrentMiniGameRequested()
            .Where(_ => _currentGameController != null)
            .Subscribe(_ => OnMiniGamePhaseFinished())
            .AddTo(this);
    }
    public override void OnEnter()
    {
        _miniGameRoot.SetActive(false);

        _miniGameStartPerformanceCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        PerformWelcomeReactionAsync().Forget();
    }
    private async UniTask<Unit> PerformWelcomeReactionAsync()
    {
        var ct = _miniGameStartPerformanceCts.Token;

        _welcomeReactionButton.gameObject.SetActive(false);
        _charaReactionView.UpdateCharacterReaction(StaffReactionCode.Welcome_1);
        await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: ct);

        _welcomeReactionButton.gameObject.SetActive(true);
        await _welcomeReactionButton.OnClickAsObservable().FirstAsync(cancellationToken: ct);

        _welcomeReactionButton.gameObject.SetActive(false);
        _charaReactionView.UpdateCharacterReaction(StaffReactionCode.Welcome_2);
        await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: ct);

        _welcomeReactionButton.gameObject.SetActive(true);
        await _welcomeReactionButton.OnClickAsObservable().FirstAsync(cancellationToken: ct);

        ShoppingModel.Instance.StartMiniGame();
        return Unit.Default;
    }
    private void OnStartMiniGameRequested(MiniGameRequestData requestData)
    {
        _currentPartType = requestData.PartType;
        _miniGameRoot.SetActive(true);
        _currentMiniGamePhaseTimeLeft = MiniGamePhaseDuration;

        // _currentPartStateView.UpdatePartData(_currentPartType, _currentPartGrade);
        _currentGameController = null;
        foreach (var miniGameController in _miniGameControllers)
        {
            if (miniGameController.ThisMiniGameType == requestData.MiniGameType)
            {
                miniGameController.OnGameLoaded();
                _currentGameController = miniGameController;
            }
            else
            {
                miniGameController.OnGameUnloaded();
            }
        }

        StartMinigame().Forget();
    }
    private async UniTask<Unit> StartMinigame()
    {
        _currentPartGrade = PartGrade.Premium;
        _currentPartStateView.UpdateBlink(UIShoppingCurrentPartStateView.BlinkSpeed.Slow);

        _currentGameController.Init(_currentPartStateView);
        _currentGameController.OnGameStartRequested();

        await RunPhaseTimeLeftAsync();

        return Unit.Default;
    }
    private void OnMiniGamePhaseFinished()
    {
        _currentGameController.OnGameFinishRequested();
    }
    private async UniTask<Unit> RunPhaseTimeLeftAsync()
    {
        _currentMiniGamePhaseTimeLeft = MiniGamePhaseDuration;


        while (_currentMiniGamePhaseTimeLeft > 0f)
        {
            var prev = _currentMiniGamePhaseTimeLeft;
            await UniTask.DelayFrame(1, cancellationToken: this.GetCancellationTokenOnDestroy());
            _currentMiniGamePhaseTimeLeft -= Time.deltaTime;

            if (prev > 3f && _currentMiniGamePhaseTimeLeft <= 3f)
            {
                _currentPartStateView.UpdateBlink(UIShoppingCurrentPartStateView.BlinkSpeed.Slow);
            }
            else if (prev > 2f && _currentMiniGamePhaseTimeLeft <= 2f)
            {
                _currentPartStateView.UpdateBlink(UIShoppingCurrentPartStateView.BlinkSpeed.Medium);
            }
            else if (prev > 1f && _currentMiniGamePhaseTimeLeft <= 1f)
            {
                _currentPartStateView.UpdateBlink(UIShoppingCurrentPartStateView.BlinkSpeed.Fast);
            }
        }

        return Unit.Default;
    }
}
