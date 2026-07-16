using AstraydeFramework.System;
using AstraydeFramework.System.UI;
using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using R3;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[LocalAssetPath("Prefabs/Common/CommonCharacterMessageWindow")]
public class UICommonCharacterMessageWindow : WindowBase
{
    [SerializeField] private Image _characterImage;
    [SerializeField] private GameObject _messageTextObject;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _confirmButton;

    private const float FadeDuration = 1f;
    private const float ButtonShowDelay = 0.5f;
    private const string CharaEmotionResourceFormat = "2D/Texture/CharaEmotion/chara_emotion_{0}_{1:D3}";

    public static async UniTask PlayAdvAsync(string code, GenderType gender)
    {
        var window = await WindowManager.OpenWindowAsync<UICommonCharacterMessageWindow>();
        await window.PlayAsync(code, gender);
        window.Close();
    }

    private async UniTask PlayAsync(string code, GenderType gender)
    {
        var ct = this.GetCancellationTokenOnDestroy();
        var records = AdvTableContainer.GetRecords(code, gender);

        _messageTextObject.SetActive(false);
        _confirmButton.gameObject.SetActive(false);

        foreach (var record in records)
        {
            // キャラ画像を差し替えてフェードイン（テクスチャが同じ場合はスキップ）
            var spritePath = string.Format(CharaEmotionResourceFormat, record.Gender, record.CharaEmotionType);
            var nextSprite = ResourceContainer.Instance.GetSpriteByPath(spritePath);
            if (_characterImage.sprite != nextSprite)
            {
                _characterImage.sprite = nextSprite;
                await FadeImageAsync(_characterImage, 0f, 1f, FadeDuration, ct);
            }

            // メッセージ表示
            _messageText.text = record.Message;
            _messageTextObject.SetActive(true);

            // 0.5秒後にボタン表示
            await UniTask.Delay(System.TimeSpan.FromSeconds(ButtonShowDelay), cancellationToken: ct);
            _confirmButton.gameObject.SetActive(true);

            // ボタンクリック待ち
            await _confirmButton.OnClickAsObservable().FirstAsync(cancellationToken: ct);

            // 次のメッセージへの準備
            _confirmButton.gameObject.SetActive(false);
            _messageTextObject.SetActive(false);
        }
    }

    private async UniTask FadeImageAsync(Image image, float from, float to, float duration, CancellationToken ct)
    {
        var elapsed = 0f;
        var color = image.color;
        color.a = from;
        image.color = color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            image.color = color;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        color.a = to;
        image.color = color;
    }
}
