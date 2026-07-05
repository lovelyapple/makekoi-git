using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BgmFader : MonoBehaviour
{
    [SerializeField] List<BgmController> BgmControllers;
    private BgmController _currentMainBgmController;

    public async UniTaskVoid PlayBgmFade(AudioClip clip)
    {
        if (_currentMainBgmController == null)
        {
            _currentMainBgmController = BgmControllers.First();
            _currentMainBgmController.SetAudioClip(clip);
            await _currentMainBgmController.FadeInAsync();
            return;
        }

        if (_currentMainBgmController.CurrentClip == clip)
        {
            if (_currentMainBgmController.IsRunningFade)
            {
                await _currentMainBgmController.FadeInAsync();
            }
            return;
        }

        var emptyOne = BgmControllers.FirstOrDefault(x => x != _currentMainBgmController);
        emptyOne.SetAudioClip(clip);

        var prev = _currentMainBgmController;
        _currentMainBgmController = emptyOne;

        await UniTask.WhenAll(
            _currentMainBgmController.FadeInAsync(),
            prev.FadeOutAsync());
    }

    public void StopImmediately()
    {
        if (_currentMainBgmController == null || _currentMainBgmController.IsEmpty)
        {
            return;
        }
        _currentMainBgmController.Stop();
    }

    public void FadeLow(float target = 0.2f)
    {
        if (_currentMainBgmController == null || _currentMainBgmController.IsEmpty)
        {
            return;
        }
        _currentMainBgmController.FadeOutAsync(target).Forget();
    }

    public void FadeUp()
    {
        if (_currentMainBgmController == null || _currentMainBgmController.IsEmpty)
        {
            return;
        }
        _currentMainBgmController.FadeInAsync(1f).Forget();
    }
}
