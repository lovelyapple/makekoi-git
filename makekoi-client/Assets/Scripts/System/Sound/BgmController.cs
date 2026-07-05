using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public class BgmController : MonoBehaviour
{
    [SerializeField] AudioSource BgmAudioResource;
    private float _targetVolume = 0;
    private const float FadeSpeed = 0.5f;
    public bool IsEmpty => BgmAudioResource.clip == null;
    public AudioClip CurrentClip => BgmAudioResource.clip;
    private CancellationTokenSource _runingTokenSource;
    public bool IsRunningFade => _runingTokenSource != null;
    private float _unClampedVolume = -1;

    private float BgmVolum
    {
        get
        {
            if (_unClampedVolume == -1)
            {
                _unClampedVolume = BgmAudioResource.volume;
            }
            return _unClampedVolume;
        }
        set
        {
            _unClampedVolume = value;
            BgmAudioResource.volume = Mathf.Clamp(_unClampedVolume, 0, 1f);
        }
    }

    private void Start()
    {
        BgmAudioResource.outputAudioMixerGroup = SoundManager.BgmMixerGroup();
    }

    public void SetAudioClip(AudioClip clip)
    {
        BgmAudioResource.clip = clip;
        BgmVolum = 0;
    }

    public void Stop()
    {
        BgmAudioResource.Stop();
        _runingTokenSource?.Cancel();
        _runingTokenSource?.Dispose();
        _runingTokenSource = null;
    }

    public async UniTask<Unit> FadeInAsync(float targetVolume = 1)
    {
        if (_runingTokenSource != null)
        {
            _runingTokenSource.Cancel();
            _runingTokenSource.Dispose();
        }

        var cts = new CancellationTokenSource();
        _runingTokenSource = cts;

        try
        {
            await FadeInInnerAsync(targetVolume, cts.Token);
        }
        finally
        {
            if (ReferenceEquals(_runingTokenSource, cts))
            {
                _runingTokenSource = null;
            }
            cts.Dispose();
        }

        return Unit.Default;
    }

    private async UniTask<Unit> FadeInInnerAsync(float targetVolume, CancellationToken fadeToken)
    {
        _targetVolume = targetVolume;
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(fadeToken, this.destroyCancellationToken);
        var token = linkedTokenSource.Token;
        BgmVolum = 0;
        BgmAudioResource.Play();
        while (BgmVolum < _targetVolume && !token.IsCancellationRequested)
        {
            BgmVolum += Time.deltaTime * FadeSpeed;
            await UniTask.Yield();
        }
        return Unit.Default;
    }

    public async UniTask<Unit> FadeOutAsync(float targetVolume = 0)
    {
        if (_runingTokenSource != null)
        {
            _runingTokenSource.Cancel();
            _runingTokenSource.Dispose();
        }

        var cts = new CancellationTokenSource();
        _runingTokenSource = cts;

        try
        {
            await FadeOutInnerAsync(targetVolume, cts.Token);
        }
        finally
        {
            if (ReferenceEquals(_runingTokenSource, cts))
            {
                _runingTokenSource = null;
            }
            cts.Dispose();
        }

        return Unit.Default;
    }

    private async UniTask<Unit> FadeOutInnerAsync(float targetVolume, CancellationToken fadeToken)
    {
        _targetVolume = targetVolume;
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(fadeToken, this.destroyCancellationToken);
        var token = linkedTokenSource.Token;
        while (BgmVolum > _targetVolume && !token.IsCancellationRequested)
        {
            BgmVolum -= Time.deltaTime * FadeSpeed;
            await UniTask.Yield();
        }

        if (token.IsCancellationRequested)
        {
            return Unit.Default;
        }

        if (BgmAudioResource != null)
        {
            BgmAudioResource.Stop();
            BgmAudioResource.clip = null;
        }

        return Unit.Default;
    }
}
