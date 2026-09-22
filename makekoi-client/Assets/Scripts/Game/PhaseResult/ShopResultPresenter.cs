using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class ShopResultPresenter : GamePhaseWindowBase
{
    [SerializeField] private ShopResultPartsView _shopResultPartsView;
    [SerializeField] private ShopResultScoreView _shopResultScoreView;
    [SerializeField] private Button _gotoHomeButton;
    private CancellationTokenSource _showResultsCts;
    private void Awake()
    {
        _gotoHomeButton.OnClickAsObservable()
            .Subscribe(_ => OnGotoHomeClicked())
            .AddTo(this);
    }
    // 1周ぶんのセッションを捨ててタイトルへ戻す
    private void OnGotoHomeClicked()
    {
        _showResultsCts?.Cancel();

        GameModel.Instance.OnReset();
        ShoppingModel.Instance.OnReset();
        PartnerSelectModel.Instance.OnReset();

        _shopResultPartsView.Close();
        _shopResultScoreView.Close();
        RequestGoToPhase(GamePhaseContentType.Title);
    }
    public override GamePhaseContentType ThisPhaseContentType => GamePhaseContentType.Result;
    public override void OnEnter()
    {
        _shopResultPartsView.Close();
        _shopResultScoreView.Close();

        _showResultsCts?.Cancel();
        _showResultsCts?.Dispose();
        _showResultsCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        ShowResultsAsync(_showResultsCts.Token).Forget();
    }
    private void OnDestroy()
    {
        _showResultsCts?.Cancel();
        _showResultsCts?.Dispose();
        _showResultsCts = null;
    }
    private async UniTask ShowResultsAsync(CancellationToken ct)
    {
        await _shopResultPartsView.ShowPartsAsync(ct);

        await UniTask.Delay(1000, cancellationToken: ct);

        // 生成済みのセルに対する操作なので、インスタンス経由でまとめて切り替える
        _shopResultPartsView.SetPerform(true);

        await UniTask.Delay(1000, cancellationToken: ct);

        await _shopResultScoreView.ShowScoreAsync(ct);
    }
}
