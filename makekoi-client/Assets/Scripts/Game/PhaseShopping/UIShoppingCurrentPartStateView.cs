using Cysharp.Threading.Tasks;
using Makekoi.PartnerCreate;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class UIShoppingCurrentPartStateView : MonoBehaviour
{
    [SerializeField] private UIPartnerClothListCell _partnerClothListCell;
    [SerializeField] private Image _timeLeftFillImage;
    [SerializeField] private Image _timeLeftFillBgImage;

    public enum BlinkSpeed { None, Slow, Medium, Fast }

    private static readonly float[] BlinkCycleDurations = { 0f, 1.2f, 0.6f, 0.25f };

    private CancellationTokenSource _blinkCts;
    private void Awake()
    {
        StopBlink();
    }
    public void UpdatePartData(PartnerPartData partData)
    {
        _partnerClothListCell.UpdateCell(partData);
    }
    // Removed unnecessary empty block
    public void UpdateBlink(BlinkSpeed speed)
    {
        StopBlink();
        _blinkCts = new CancellationTokenSource();
        if (speed == BlinkSpeed.None)
        {
            _timeLeftFillBgImage.gameObject.SetActive(false);
        }
        else
        {
            _timeLeftFillBgImage.gameObject.SetActive(true);
            BlinkAsync(BlinkCycleDurations[(int)speed], _blinkCts.Token).Forget();
        }
    }

    public void StopBlink()
    {
        _blinkCts?.Cancel();
        _blinkCts?.Dispose();
        _blinkCts = null;

        var color = _timeLeftFillBgImage.color;
        color.a = 1f;
        _timeLeftFillBgImage.color = color;
    }

    private async UniTaskVoid BlinkAsync(float cycleDuration, CancellationToken ct)
    {
        var elapsed = 0f;
        var color = _timeLeftFillBgImage.color;
        while (!ct.IsCancellationRequested)
        {
            elapsed += Time.deltaTime;
            color.a = (Mathf.Sin(elapsed / cycleDuration * Mathf.PI * 2f) + 1f) * 0.5f;
            _timeLeftFillBgImage.color = color;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }
}
