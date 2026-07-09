using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGenderSelectConfirmPerformanceView : MonoBehaviour
{
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _characterImage;
    [SerializeField] private GameObject _messageTextObject;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _confirmButton;
    private void Awake()
    {
        _confirmButton.onClick.AddListener(() =>
        {
            // Perform any necessary actions when the confirm button is clicked
            Debug.Log("Confirm button clicked!");
        });
    }
    public void Setup(Sprite backgroundSprite, string message)
    {
        _backgroundImage.sprite = backgroundSprite;
        _characterImage.sprite = backgroundSprite;
        _messageText.text = message;

        _characterImage.color = new Color(1f, 1f, 1f, 0f);
        _messageTextObject.gameObject.SetActive(false);
    }
    public async UniTask<Unit> ShowAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        this.gameObject.SetActive(true);

        // 1秒かけて_characterImageフェードイン
        var elapsed = 0f;
        const float duration = 2f;
        var color = _characterImage.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            _characterImage.color = color;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        color.a = 1f;
        _characterImage.color = color;

        _messageTextObject.SetActive(true);

        // _confirmButton のクリックされること待つ
        await _confirmButton.OnClickAsObservable().FirstAsync(cancellationToken: ct);
        gameObject.SetActive(false);
        return Unit.Default;
    }
}
