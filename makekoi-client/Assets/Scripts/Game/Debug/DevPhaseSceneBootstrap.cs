using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using R3;
using UnityEngine;

/// <summary>
/// 各フェーズを単独で動かすための開発用シーンのエントリポイント。
/// 本番の UIRootController の代わりに、対象の UIGameContentHolder 1つだけを起動する。
/// </summary>
public class DevPhaseSceneBootstrap : MonoBehaviour
{
    [SerializeField] private UIGameContentHolder _targetHolder;

    [Header("Dummy Session Data")]
    [SerializeField] private GenderType _gender = GenderType.Female;
    [SerializeField] private int _playerMoney = 30000;

    private void Start()
    {
        RunAsync().Forget();
    }

    private async UniTaskVoid RunAsync()
    {
        if (_targetHolder == null)
        {
            Debug.LogError("[DevPhaseSceneBootstrap] _targetHolder が未設定です", this);
            return;
        }

        var ct = this.GetCancellationTokenOnDestroy();
        // UIGameContentHolder.Start() でのコンテンツ生成を待つ
        await UniTask.WaitUntil(() => _targetHolder.IsContentLoaded, cancellationToken: ct);

        SetupDummySession();

        // 単独シーンなので遷移先が存在しない。ログだけ出す。
        _targetHolder.OnRequestGoToNextPhase()
            .Subscribe(nextPhase => Debug.Log($"[DevPhaseSceneBootstrap] RequestGoToNextPhase -> {nextPhase} (開発用シーンでは遷移しません)"))
            .AddTo(this);

        SyncFooter();
        _targetHolder.Content.OnEnter();
    }

    /// <summary>本番では UIRootController がやっているフッタータブの選択表示を代行する。</summary>
    private void SyncFooter()
    {
        var footerController = FindFirstObjectByType<UIFooterController>(FindObjectsInactive.Include);
        if (footerController != null)
        {
            footerController.OnTypeSelected(_targetHolder.ButtonType);
        }
    }

    private void SetupDummySession()
    {
        GameModel.Instance.OnReset();
        GameModel.Instance.SessionData.PlayerMoney = _playerMoney;
        GameModel.Instance.UpdateGenderType(_gender);

        if (_targetHolder.ButtonType == GamePhaseContentType.Shopping)
        {
            // 通常は CharaSelect フェーズを経て初期化される部分を肩代わりする
            ShoppingModel.Instance.Init(_gender);
            ShoppingModel.Instance.StartShopping();
        }
    }
}
