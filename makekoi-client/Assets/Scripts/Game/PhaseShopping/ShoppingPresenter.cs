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
    // interface のままだと Unity がシリアライズできず Inspector に出ないため、基底クラスで受ける
    [SerializeField] private List<MiniGameBase> _miniGameControllers;
    [SerializeField] private UIMiniGameResultView _miniGameResultView;
    [SerializeField] private MiniGamePerformController _miniGamePerformController;
    [SerializeField] private Button _welcomeReactionButton;
    [SerializeField] private GameObject _miniGameRoot;
    [SerializeField] private ShoppingFailedView _shoppingFailedView;
    #region MiniGame_phase
    private const float DogezaPhaseDuration = 5f;
    private const float TimingTapPhaseDuration = 10f;
    private const float JyankenPhaseDuration = 5f;
    // ロード時のアイドル台詞を見せてからゲームを開始するまでの間
    private const float MiniGameIdleDuration = 1.5f;
    private float _currentMiniGamePhaseTimeLeft = 0f;
    private float _currentMiniGamePhaseDuration;
    private PartType _currentPartType;
    private PartGrade _currentPartGrade;
    private IMiniGame _currentGameController;
    private CancellationTokenSource _miniGamePhaseCts;
    private bool _isMiniGamePhaseFinished;
    #endregion
    CancellationTokenSource _miniGameStartPerformanceCts;
    private void Awake()
    {
        ShoppingModel.Instance.StartMiniGameObservable()
            .Subscribe(OnStartMiniGameRequested)
            .AddTo(this);


        foreach (var miniGameController in _miniGameControllers)
        {
            miniGameController.StaffReactionRequestedObservable()
                .Subscribe(code => _charaReactionView.UpdateCharacterReaction(code))
                .AddTo(this);
        }

        // 全部位を買い終わったらリザルトへ送る
        ShoppingModel.Instance.ShoppingFinishedObservable()
            .Subscribe(_ => RequestGoToNextPhase())
            .AddTo(this);

        DebugModel.Instance.OnFinishCurrentMiniGameRequested()
            .Where(_ => _currentGameController != null)
            .Subscribe(_ => OnMiniGamePhaseFinished())
            .AddTo(this);
    }
    public override GamePhaseContentType ThisPhaseContentType => GamePhaseContentType.Shopping;
    public override void OnEnter()
    {
        _miniGameRoot.SetActive(false);
        _miniGameResultView.Close();
        _shoppingFailedView.gameObject.SetActive(false);

        // 品揃えはキャラ確定時に CharacterSelectPresenter で抽選済み
        // (Dev シーンでは DevPhaseSceneBootstrap が肩代わりしている)

        _miniGameStartPerformanceCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        PerformWelcomeReactionAsync().Forget();
    }
    private async UniTask<Unit> PerformWelcomeReactionAsync()
    {
        var ct = _miniGameStartPerformanceCts.Token;

        _welcomeReactionButton.gameObject.SetActive(false);
        _charaReactionView.UpdateCharacterReaction(StaffReactionCode.Welcome_1);
        await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: ct);

        await OnClickNext();
        _charaReactionView.UpdateCharacterReaction(StaffReactionCode.Welcome_2);
        await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: ct);

        await OnClickNext();
        ShoppingModel.Instance.StartMiniGame();
        return Unit.Default;
    }
    private async UniTask<Unit> OnClickNext()
    {
        var ct = _miniGameStartPerformanceCts.Token;
        _welcomeReactionButton.gameObject.SetActive(true);
        await _welcomeReactionButton.OnClickAsObservable().FirstAsync(cancellationToken: ct);

        _welcomeReactionButton.gameObject.SetActive(false);
        return Unit.Default;
    }
    private void OnStartMiniGameRequested(MiniGameRequestData requestData)
    {
        _currentPartType = requestData.PartType;
        _miniGameRoot.SetActive(true);
        _currentMiniGamePhaseDuration = GetMiniGamePhaseDuration(requestData.MiniGameType);
        _currentMiniGamePhaseTimeLeft = _currentMiniGamePhaseDuration;

        // 狙う対象として最上位グレードのパーツを出しておく
        _currentGameController = null;
        PerformNextPartAsync(requestData).Forget();
    }
    private async UniTask<Unit> PerformNextPartAsync(MiniGameRequestData miniGameRequestData)
    {
        // Implement the logic for starting the next part of the mini-game
        var nextTargetPart = miniGameRequestData.Part;
        if (nextTargetPart != null)
        {
            _currentPartStateView.UpdateNextPartData(nextTargetPart);
            await _currentPartStateView.UpdateCostMoneyRollerAsync(nextTargetPart);
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(1),
             cancellationToken: this.GetCancellationTokenOnDestroy());

        await OnClickNext();

        if (nextTargetPart != null)
        {
            _currentPartStateView.UpdatePartData(nextTargetPart);
        }

        foreach (var miniGameController in _miniGameControllers)
        {
            if (miniGameController.ThisMiniGameType == miniGameRequestData.MiniGameType)
            {
                miniGameController.OnGameLoaded(miniGameRequestData);
                _currentGameController = miniGameController;
            }
            else
            {
                miniGameController.OnGameUnloaded();
            }
        }

        StartMinigame().Forget();

        return Unit.Default;
    }
    private async UniTask<Unit> StartMinigame()
    {
        _currentPartGrade = PartGrade.Premium;
        _currentPartStateView.UpdateBlink(UIShoppingCurrentPartStateView.BlinkSpeed.Slow);

        // _currentGameController.Init(_currentPartStateView);
        _miniGameResultView.Close();

        _isMiniGamePhaseFinished = false;
        _miniGamePhaseCts?.Cancel();
        _miniGamePhaseCts?.Dispose();
        _miniGamePhaseCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        await _miniGamePerformController.PerformMiniGame(_currentGameController.ThisMiniGameType);

        // OnGameLoaded で出したアイドル台詞を読ませてから開始する
        // (待機中にデバッグ操作で終了された場合はキャンセルされるので、そのまま抜ける)
        if (await UniTask.Delay(System.TimeSpan.FromSeconds(MiniGameIdleDuration), cancellationToken: _miniGamePhaseCts.Token)
                .SuppressCancellationThrow())
        {
            return Unit.Default;
        }
        _charaReactionView.CloseReactionMsgBox();
        _currentGameController.OnGameStartRequested();
        // デバッグ等で途中終了した場合はキャンセルされるので、その場合は下の終了処理を走らせない
        var isCanceled = await RunPhaseTimeLeftAsync(_miniGamePhaseCts.Token).SuppressCancellationThrow();
        if (!isCanceled.IsCanceled)
        {
            OnMiniGamePhaseFinished();
        }
        return Unit.Default;
    }
    // 制限時間切れ・デバッグ操作のどちらから来ても一度だけ走るようにする
    private void OnMiniGamePhaseFinished()
    {
        if (_isMiniGamePhaseFinished) return;
        _isMiniGamePhaseFinished = true;
        _miniGamePhaseCts?.Cancel();

        FinishMiniGamePhaseAsync().Forget();
    }
    private async UniTask<Unit> FinishMiniGamePhaseAsync()
    {
        _currentPartStateView.UpdateBlink(UIShoppingCurrentPartStateView.BlinkSpeed.None);

        var gameController = _currentGameController;
        _currentGameController = null;
        gameController.OnGameFinishRequested();
        var result = gameController.CurrentMiniGameResult;

        if (result == null)
        {
            Debug.LogError($"[ShoppingPresenter] {_currentPartType} のミニゲーム結果が取得できませんでした");
            return Unit.Default;
        }

        await RunMiniGamePahseReusltConfirmAsync(result);

        // 払った結果マイナスになったら、そこで買い物は打ち切り
        if (GameModel.Instance.SessionData.Money < 0)
        {
            await PerformShoppingFailedAsync();
            return Unit.Default;
        }

        ShoppingModel.Instance.OnMiniGameFinished(result);
        return Unit.Default;
    }
    private async UniTask<Unit> PerformShoppingFailedAsync()
    {
        // mini_game_failed は _miniGameRoot の子なので、root は出したままにする
        // 失敗パネルを読んでいる間に消えないよう、自動で閉じない
        _charaReactionView.UpdateCharacterReaction(StaffReactionCode.MiniGame_Failed, autoClose: false);
        _shoppingFailedView.gameObject.SetActive(true);

        await _shoppingFailedView.OnBackToTopClicked();

        _shoppingFailedView.gameObject.SetActive(false);
        _charaReactionView.CloseReactionMsgBox();
        RequestGoToPhase(GamePhaseContentType.Title);
        return Unit.Default;
    }
    private async UniTask<Unit> RunPhaseTimeLeftAsync(CancellationToken ct)
    {
        _currentMiniGamePhaseTimeLeft = _currentMiniGamePhaseDuration;


        while (_currentMiniGamePhaseTimeLeft > 0f)
        {
            var prev = _currentMiniGamePhaseTimeLeft;
            await UniTask.DelayFrame(1, cancellationToken: ct);
            _currentMiniGamePhaseTimeLeft -= Time.deltaTime;
            _currentPartStateView.UpdateFillAmount(_currentMiniGamePhaseTimeLeft / _currentMiniGamePhaseDuration);

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

    private static float GetMiniGamePhaseDuration(MiniGameType miniGameType) => miniGameType switch
    {
        MiniGameType.TimeTap => TimingTapPhaseDuration,
        MiniGameType.Jyanken => JyankenPhaseDuration,
        _ => DogezaPhaseDuration,
    };
    private async UniTask<Unit> RunMiniGamePahseReusltConfirmAsync(MiniGameResultData result)
    {
        _charaReactionView.UpdateCharacterReaction(GetResultReactionCode(result.ResultPart.Grade));
        await _miniGameResultView.ShowAndWaitConfirmAsync(result, this.GetCancellationTokenOnDestroy());

        GameModel.Instance.OnMoneyPaid(result.CostMoney);

        _charaReactionView.CloseReactionMsgBox();
        return Unit.Default;
    }
    private static StaffReactionCode GetResultReactionCode(PartGrade grade) => grade switch
    {
        PartGrade.Premium => StaffReactionCode.Result_Premium,
        PartGrade.Mid => StaffReactionCode.Result_Mid,
        _ => StaffReactionCode.Result_Budget,
    };
}
