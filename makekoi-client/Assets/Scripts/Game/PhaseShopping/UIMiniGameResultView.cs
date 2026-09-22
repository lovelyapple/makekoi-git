using System.Threading;
using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ミニゲーム終了時に、獲得したパーツを表示して確認ボタンが押されるまで待つビュー
public class UIMiniGameResultView : MonoBehaviour
{
    [SerializeField] private RectTransform _popupTransform;
    [SerializeField] private Image _partImage;
    [SerializeField] private TextMeshProUGUI _partNameText;


    [SerializeField] private UINumberRollerGroup _priceRoller;
    [SerializeField] private GameObject _costArrowObject;
    [SerializeField] private UINumberRollerGroup _paidRoller;
    [SerializeField] private Button _confirmButton;

    private const float PopupDuration = 0.25f;
    private const float PopupStartScale = 0.7f;
    private const float PopupOvershootScale = 1.05f;

    // PartGrade の並び (Premium=0, Mid=1, Budget=2) に対応
    private static readonly string[] GradeLabels = { "高級品", "そこそこ", "お買い得" };

    // 初期化時の非表示は ShoppingPresenter 側から Close() を呼んで行う
    // (Awake で閉じると、初回 SetActive(true) のタイミングで Awake が走って閉じてしまうため)
    public void Close()
    {
        _costArrowObject.SetActive(false);
        _priceRoller.SetValueImmediate(0);
        _paidRoller.SetValueImmediate(0);

        _priceRoller.gameObject.SetActive(false);
        _paidRoller.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    // 結果を表示し、確認ボタンが押されたら閉じて完了する
    public async UniTask ShowAndWaitConfirmAsync(MiniGameResultData result, CancellationToken ct)
    {
        UpdateResult(result);

        gameObject.SetActive(true);
        _confirmButton.interactable = false;
        await PlayPopupAsync(ct);

        await UniTask.Delay(System.TimeSpan.FromSeconds(1), cancellationToken: ct);

        _priceRoller.gameObject.SetActive(true);
        await _priceRoller.RollToAsync((int)result.BasePrice, ct);

        await UniTask.Delay(System.TimeSpan.FromSeconds(1), cancellationToken: ct);
        _costArrowObject.SetActive(true);
        await UniTask.Delay(System.TimeSpan.FromSeconds(1), cancellationToken: ct);


        _paidRoller.gameObject.SetActive(true);
        await _paidRoller.RollToAsync((int)result.CostMoney, ct);

        _confirmButton.interactable = true;
        await _confirmButton.OnClickAsObservable().FirstAsync(cancellationToken: ct);

        Close();
    }

    private void UpdateResult(MiniGameResultData result)
    {
        var partData = result.ResultPart;
        if (partData == null)
        {
            Debug.LogWarning($"[UIMiniGameResultView] {result.PartType} の獲得パーツがありません");
            return;
        }

        if (_partImage != null)
        {
            _partImage.sprite = partData.PartSprite;
        }
        if (_partNameText != null)
        {
            _partNameText.text = partData.PartMasterRecord != null ? partData.PartMasterRecord.Name : string.Empty;
        }
    }

    private async UniTask PlayPopupAsync(CancellationToken ct)
    {
        if (_popupTransform == null) return;

        var elapsed = 0f;
        while (elapsed < PopupDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / PopupDuration);
            // 前半で少し行き過ぎてから等倍に戻す
            var scale = t < 0.7f
                ? Mathf.Lerp(PopupStartScale, PopupOvershootScale, t / 0.7f)
                : Mathf.Lerp(PopupOvershootScale, 1f, (t - 0.7f) / 0.3f);
            _popupTransform.localScale = Vector3.one * scale;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        _popupTransform.localScale = Vector3.one;
    }
}
