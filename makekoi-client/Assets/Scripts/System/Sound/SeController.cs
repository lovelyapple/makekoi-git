using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Audio;

public class SeController : MonoBehaviour
{
    private const float _startTimeOutSec = 3f;
    private const float _endTimeOutSec = 5f;
    [SerializeField] AudioSource Source;
    private AudioMixerGroup _mixerGroup;
    public bool IsEmpty => !Source.isPlaying;

    public void PlaySe(AudioClip audioClip)
    {
        _mixerGroup = SoundManager.SeMixerGroup();
        Source.outputAudioMixerGroup = _mixerGroup;
        PlayAsync(audioClip).Forget();
    }

    public void PlayVoice(AudioClip audioClip)
    {
        _mixerGroup = SoundManager.VoiceMixerGroup();
        Source.outputAudioMixerGroup = _mixerGroup;
        PlayAsync(audioClip).Forget();
    }

    private async UniTask<Unit> PlayAsync(AudioClip audioClip)
    {
        gameObject.SetActive(true);
        var destroyToken = this.destroyCancellationToken;

        try
        {
            Source.PlayOneShot(audioClip);
            var timeOutTask1 = UniTask.Delay(TimeSpan.FromSeconds(_startTimeOutSec), cancellationToken: destroyToken);
            var waitStart = UniTask.WaitUntil(() => Source.isPlaying, cancellationToken: destroyToken);
            await UniTask.WhenAny(timeOutTask1, waitStart);

            var timeOutTask2 = UniTask.Delay(TimeSpan.FromSeconds(_endTimeOutSec), cancellationToken: destroyToken);
            var waitEnd = UniTask.WaitUntil(() => !Source.isPlaying, cancellationToken: destroyToken);
            await UniTask.WhenAny(timeOutTask2, waitEnd);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"se play error: {ex}");
        }
        finally
        {
            if (!destroyToken.IsCancellationRequested)
            {
                if (Source != null)
                {
                    Source.Stop();
                    Source.clip = null;
                }

                if (this != null && gameObject)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        return Unit.Default;
    }
}
