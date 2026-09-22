using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using UnityEngine;

// リザルト画面で、部位ごとの買い物結果セルを下から上へ順番に出していくビュー
public class ShopResultPartsView : MonoBehaviour
{
    [SerializeField] private RectTransform _partsRoot;
    // root_parts に置いてあるセルを雛形として複製する (雛形自体は非表示にしておく)
    [SerializeField] private UIClothPartResultCell _clothPartResultCell;

    // 1セル出てから次のセルが出るまでの間
    private const float CellAppearInterval = 0.2f;
    private const float CellPopupDuration = 0.2f;
    private const float CellPopupStartScale = 0.7f;
    private const float CellPopupOvershootScale = 1.05f;

    private readonly List<CanvasGroup> _cellGroups = new List<CanvasGroup>();
    private readonly List<UIClothPartResultCell> _cells = new List<UIClothPartResultCell>();

    public void Close()
    {
        ClearCells();
        _clothPartResultCell.gameObject.SetActive(false);
    }

    // 結果セルを並べ、下のセルから順に出していく
    public async UniTask ShowPartsAsync(CancellationToken ct)
    {
        BuildCells();

        // VerticalLayoutGroup が詰め直さないよう、非表示は SetActive ではなく alpha で行う
        for (var i = _cellGroups.Count - 1; i >= 0; i--)
        {
            await PlayCellAppearAsync(_cellGroups[i], ct);
            if (i > 0)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(CellAppearInterval), cancellationToken: ct);
            }
        }
    }

    private void BuildCells()
    {
        ClearCells();
        _clothPartResultCell.gameObject.SetActive(false);

        var results = ShoppingModel.Instance.Results;
        foreach (PartType partType in System.Enum.GetValues(typeof(PartType)))
        {
            if (!results.TryGetValue(partType, out var result) || result == null)
            {
                // 買い物していない部位は行ごと出さない
                continue;
            }

            var cell = Instantiate(_clothPartResultCell, _partsRoot);
            cell.gameObject.SetActive(true);
            cell.OnReset();
            cell.SetPartResultData(result);

            var canvasGroup = cell.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = cell.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            _cellGroups.Add(canvasGroup);
            _cells.Add(cell);
        }
    }

    private void ClearCells()
    {
        foreach (var cellGroup in _cellGroups)
        {
            if (cellGroup != null)
            {
                Destroy(cellGroup.gameObject);
            }
        }
        _cellGroups.Clear();
        _cells.Clear();
    }

    /// <summary>全セルの部位スコアを出す (演出の後半用)。</summary>
    public void SetPerform(bool isScoreVisible)
    {
        foreach (var cell in _cells)
        {
            cell.SetPerform(isScoreVisible);
        }
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private GenderType _debugGender = GenderType.Female;

    // 買い物フェーズを通さずにリザルト演出だけ確認する
    [ContextMenu("Debug Play With Dummy Results")]
    private void DebugPlayWithDummyResults()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ShopResultPartsView] 再生中のみ実行できます", this);
            return;
        }

        ShoppingModel.Instance.SetupDebugResults(_debugGender);
        ShowPartsAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    [ContextMenu("Debug Close")]
    private void DebugClose()
    {
        Close();
    }
#endif

    private async UniTask PlayCellAppearAsync(CanvasGroup cellGroup, CancellationToken ct)
    {
        var rectTransform = (RectTransform)cellGroup.transform;
        cellGroup.alpha = 1f;

        var elapsed = 0f;
        while (elapsed < CellPopupDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / CellPopupDuration);
            // 前半で少し行き過ぎてから等倍に戻す
            var scale = t < 0.7f
                ? Mathf.Lerp(CellPopupStartScale, CellPopupOvershootScale, t / 0.7f)
                : Mathf.Lerp(CellPopupOvershootScale, 1f, (t - 0.7f) / 0.3f);
            rectTransform.localScale = Vector3.one * scale;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        rectTransform.localScale = Vector3.one;
    }
}
