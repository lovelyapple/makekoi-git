using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
public class TitleView : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Image _buttonImage;
    private bool _isButtonClickable = false;
    public Observable<Unit> OnStartButtonClicked =>
        _startButton.OnClickAsObservable()
        .Where(_ => _isButtonClickable);

    public void OnReset()
    {
        _isButtonClickable = true;
    }
    public async UniTask<Unit> FadeInButtonAsync(CancellationToken cancellationToken)
    {
        _isButtonClickable = false;
        var color = _buttonImage.color;
        var elapsed = 0f;
        const float duration = 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;
            color.a = Mathf.PingPong(t * 10f, 1f);
            _buttonImage.color = color;
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        color.a = 1f;
        _buttonImage.color = color;
        return Unit.Default;
    }
}
