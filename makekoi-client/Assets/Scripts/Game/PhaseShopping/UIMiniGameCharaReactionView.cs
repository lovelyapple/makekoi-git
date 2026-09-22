using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMiniGameCharaReactionView : MonoBehaviour
{
    [SerializeField] private Image _characterImage;
    [SerializeField] private TextMeshProUGUI _reactionMessageText;

    /// <summary>台詞の枠。閉じるときは枠ごと消す。未設定ならテキストだけ消す。</summary>
    [SerializeField] private GameObject _reactionMessageBox;

    /// <summary>台詞を出してから自動で閉じるまでの秒数。0 以下で自動で閉じない。</summary>
    [SerializeField] private float _autoCloseSeconds = 3f;

    private CancellationTokenSource _autoCloseCts;

    private GameObject MessageBox =>
        _reactionMessageBox != null ? _reactionMessageBox : _reactionMessageText.gameObject;

    private void OnDestroy()
    {
        CancelAutoClose();
    }

    /// <param name="autoClose">false を渡すと、閉じるまで出したままにする。</param>
    public void UpdateCharacterReaction(StaffReactionCode reactionCode, bool autoClose = true)
    {
        var record = StaffReactionTableContainer.GetRandomRecord(reactionCode);
        if (record == null)
        {
            Debug.LogWarning($"[UIMiniGameCharaReactionView] StaffReactionMaster に {reactionCode} のレコードがありません");
            return;
        }
        var spriteResource = ResourceContainer.Instance.GetSpriteByPath(record.TexturePath);
        _characterImage.sprite = spriteResource;

        MessageBox.SetActive(true);
        _reactionMessageText.text = record.Message;

        if (!autoClose)
        {
            CancelAutoClose();
            return;
        }

        // 続けて別の台詞が来たら、そこから数え直す
        RestartAutoClose();
    }

    public void CloseReactionMsgBox()
    {
        CancelAutoClose();
        MessageBox.SetActive(false);
    }

    private void RestartAutoClose()
    {
        CancelAutoClose();
        if (_autoCloseSeconds <= 0f) return;

        _autoCloseCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        AutoCloseAsync(_autoCloseSeconds, _autoCloseCts.Token).Forget();
    }

    private async UniTaskVoid AutoCloseAsync(float seconds, CancellationToken ct)
    {
        // 待っている間に閉じられたり別の台詞に差し替わったらキャンセルされる
        if (await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct).SuppressCancellationThrow()) return;

        MessageBox.SetActive(false);
    }

    private void CancelAutoClose()
    {
        _autoCloseCts?.Cancel();
        _autoCloseCts?.Dispose();
        _autoCloseCts = null;
    }
}
