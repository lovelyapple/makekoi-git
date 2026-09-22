using System.Threading;
using Makekoi.PartnerCreate;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

// リザルト画面のスコア内訳を1項目ずつ出して、そのつど合計に足していくビュー
public class ShopResultScoreView : MonoBehaviour
{
    [System.Serializable]
    private class ScoreLine
    {
        // 「値引き」などの見出し。スコアと同時に出す。
        public TextMeshProUGUI FixText;
        public TextMeshProUGUI ScoreText;

        public void SetActive(bool isActive)
        {
            if (FixText != null) FixText.gameObject.SetActive(isActive);
            if (ScoreText != null) ScoreText.gameObject.SetActive(isActive);
        }
    }

    [SerializeField] private ScoreLine _partLine;
    [SerializeField] private ScoreLine _moneyLeftLine;
    [SerializeField] private ScoreLine _gradeBonusLine;
    [SerializeField] private ScoreLine _wishBonusLine;
    [SerializeField] private GameObject _lineObject;
    [SerializeField] private TextMeshProUGUI _totalScoreText;

    // 1項目のスコアを数え上げる時間
    private const float CountUpDuration = 0.4f;
    // 次の項目に移るまでの間
    private const float LineInterval = 0.3f;
    private const float PopupDuration = 0.2f;
    private const float PopupStartScale = 0.7f;
    private const float PopupOvershootScale = 1.05f;

    public void Close()
    {
        _partLine.SetActive(false);
        _moneyLeftLine.SetActive(false);
        _gradeBonusLine.SetActive(false);
        _wishBonusLine.SetActive(false);

        if (_lineObject != null) _lineObject.SetActive(false);
        if (_totalScoreText != null)
        {
            _totalScoreText.gameObject.SetActive(false);
            _totalScoreText.text = "0";
        }
    }

    // 内訳を1項目ずつ出し、そのつど合計に反映する
    public async UniTask<ShopResultScore> ShowScoreAsync(CancellationToken ct)
    {
        var score = ShopResultScore.Calculate();

        Close();
        if (_lineObject != null) _lineObject.SetActive(true);
        if (_totalScoreText != null) _totalScoreText.gameObject.SetActive(true);

        var total = 0;
        total = await ShowLineAsync(_partLine, score.PartScore, total, ct);
        total = await ShowLineAsync(_moneyLeftLine, score.MoneyLeftScore, total, ct);
        total = await ShowLineAsync(_gradeBonusLine, score.GradeBonus, total, ct);
        total = await ShowLineAsync(_wishBonusLine, score.WishBonus, total, ct);

        return score;
    }

    // 1項目を出して数え上げ、同じ時間で合計にも足し込む。戻り値は足し込んだあとの合計。
    private async UniTask<int> ShowLineAsync(ScoreLine line, int value, int total, CancellationToken ct)
    {
        line.SetActive(true);
        if (line.ScoreText != null)
        {
            line.ScoreText.text = "0";
            PlayPopupAsync(line.ScoreText.rectTransform, ct).Forget();
        }

        var nextTotal = total + value;
        await CountUpAsync(t =>
        {
            if (line.ScoreText != null)
            {
                line.ScoreText.text = Mathf.RoundToInt(Mathf.Lerp(0, value, t)).ToString();
            }
            if (_totalScoreText != null)
            {
                _totalScoreText.text = Mathf.RoundToInt(Mathf.Lerp(total, nextTotal, t)).ToString();
            }
        }, ct);

        await UniTask.Delay(System.TimeSpan.FromSeconds(LineInterval), cancellationToken: ct);
        return nextTotal;
    }

    private static async UniTask CountUpAsync(System.Action<float> onProgress, CancellationToken ct)
    {
        var elapsed = 0f;
        while (elapsed < CountUpDuration)
        {
            elapsed += Time.deltaTime;
            onProgress(Mathf.Clamp01(elapsed / CountUpDuration));
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        onProgress(1f);
    }

    private static async UniTask PlayPopupAsync(RectTransform rectTransform, CancellationToken ct)
    {
        if (rectTransform == null) return;

        var elapsed = 0f;
        while (elapsed < PopupDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / PopupDuration);
            // 前半で少し行き過ぎてから等倍に戻す
            var scale = t < 0.7f
                ? Mathf.Lerp(PopupStartScale, PopupOvershootScale, t / 0.7f)
                : Mathf.Lerp(PopupOvershootScale, 1f, (t - 0.7f) / 0.3f);
            rectTransform.localScale = Vector3.one * scale;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        rectTransform.localScale = Vector3.one;
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private GenderType _debugGender = GenderType.Female;

    // 買い物フェーズを通さずにスコア演出だけ確認する
    [ContextMenu("Debug Play With Dummy Results")]
    private void DebugPlayWithDummyResults()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ShopResultScoreView] 再生中のみ実行できます", this);
            return;
        }

        ShoppingModel.Instance.SetupDebugResults(_debugGender);
        ShowScoreAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    [ContextMenu("Debug Close")]
    private void DebugClose()
    {
        Close();
    }
#endif
}
