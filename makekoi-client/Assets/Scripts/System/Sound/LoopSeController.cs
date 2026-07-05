using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Audio;

public class LoopSeController : MonoBehaviour
{
    [SerializeField] AudioSource Source;
    private AudioMixerGroup _mixerGroup;
    public bool IsEmpty => !Source.isPlaying;
    private CancellationTokenSource _fadeSource;
    private const float NormalFadeSpeed = 0.3f;
    private const float FastFadeSpeed = 1f;

    public void Init(AudioClip audioClip)
    {
        _mixerGroup = SoundManager.SeMixerGroup();
        Source.clip = audioClip;
        Source.Play();
        Source.outputAudioMixerGroup = _mixerGroup;
        Source.volume = 0;
    }

    public void Play(bool play)
    {
        gameObject.SetActive(play);
    }

    public void VolumUpDiff(float diff)
    {
        var target = Mathf.Clamp(Source.volume + diff, 0, 1);
        var campledDiff = target - Source.volume;

        if (campledDiff * campledDiff < 0.001f)
        {
            return;
        }

        if (campledDiff > 0)
        {
            VolumUpFade(target);
        }
        else
        {
            VolumDownFade(target);
        }
    }

    public void VolumUpFade(float target, bool fadeFast = false)
    {
        if (_fadeSource != null)
        {
            _fadeSource.Cancel();
        }

        _fadeSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, this.destroyCancellationToken);

        target = target > 1 ? 1 : target;
        UpAsync(target, fadeFast ? FastFadeSpeed : NormalFadeSpeed).Forget();
    }

    private async UniTask<Unit> UpAsync(float target, float fadeSpeed)
    {
        var token = _fadeSource.Token;
        while (Source.volume < target)
        {
            if (token.IsCancellationRequested)
            {
                return Unit.Default;
            }
            Source.volume += Time.deltaTime * fadeSpeed;
            await UniTask.Yield();
        }

        if (!token.IsCancellationRequested)
        {
            Source.volume = target;
        }

        _fadeSource = null;
        return Unit.Default;
    }

    public void VolumDownFade(float target, bool fadeFast = false)
    {
        if (_fadeSource != null)
        {
            _fadeSource.Cancel();
        }

        _fadeSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, this.destroyCancellationToken);

        target = target < 0 ? 0 : target;
        DownAsync(target, fadeFast ? FastFadeSpeed : NormalFadeSpeed).Forget();
    }

    private async UniTask<Unit> DownAsync(float target, float fadeSpeed)
    {
        var token = _fadeSource.Token;
        while (Source.volume > target)
        {
            if (token.IsCancellationRequested)
            {
                return Unit.Default;
            }
            Source.volume -= Time.deltaTime * fadeSpeed;
            await UniTask.Yield();
        }

        if (!token.IsCancellationRequested)
        {
            Source.volume = target;
        }

        _fadeSource = null;
        return Unit.Default;
    }
}
