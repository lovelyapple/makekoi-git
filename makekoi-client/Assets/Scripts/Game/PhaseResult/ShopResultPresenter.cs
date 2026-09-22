using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;

public class ShopResultPresenter : GamePhaseWindowBase
{
    [SerializeField] private ShopResultPartsView _shopResultPartsView;
    [SerializeField] private ShopResultScoreView _shopResultScoreView;
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
