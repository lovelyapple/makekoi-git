using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;

    public AudioSource seSource;

    [SerializeField] AudioMixer GameAudioMixer;
    [SerializeField] SoundsContainer SeHolder;
    [SerializeField] BgmFader BgmFader;
    [SerializeField] SeRootController SeRootController;

    public void Awake()
    {
        _instance = this;
        SetBgmVolume(1f);
        SetSeVolume(1f);
    }

    public static AudioMixerGroup BgmMixerGroup() => _instance.GameAudioMixer.FindMatchingGroups("BGM")[0];
    public static AudioMixerGroup SeMixerGroup() => _instance.GameAudioMixer.FindMatchingGroups("SE")[0];
    public static AudioMixerGroup VoiceMixerGroup() => _instance.GameAudioMixer.FindMatchingGroups("Voice")[0];

    public static AudioClip GetCommonSeByType(CommonSeType seType)
    {
        return _instance.SeHolder.GetSeData(seType.ToString());
    }

    public static AudioClip GetCommonSeByType(string seType)
    {
        return _instance.SeHolder.GetSeData(seType);
    }

    public static AudioClip GetBgmByType(string bgmType)
    {
        return _instance.SeHolder.GetBgmData(bgmType);
    }

    public static void PlaySE(AudioClip clip)
    {
        _instance.SeRootController.PlaySe(clip);
    }

    public static void PlaySeByType(CommonSeType seType)
    {
        _instance.SeRootController.PlaySe(GetCommonSeByType(seType));
    }

    public static void PlaySeByType(string seType)
    {
        _instance.SeRootController.PlaySe(GetCommonSeByType(seType));
    }

    public static void PlayBgm(AudioClip clip)
    {
        _instance.BgmFader.PlayBgmFade(clip).Forget();
    }

    public static void StopBgm()
    {
        _instance.BgmFader.FadeLow(0f);
    }


    public static void SetBgmVolume(float v)
    {
        var db = 0f;
        if (v <= 0.5f)
        {
            var d = v / 0.5f;
            db = Mathf.Lerp(-80, 0, d);
        }
        else
        {
            var d = (v - 0.5f) / 0.5f;
            db = Mathf.Lerp(0, 20, d);
        }
        _instance.GameAudioMixer.SetFloat("BgmVolume", db);
    }

    public static void SetSeVolume(float v)
    {
        var db = 0f;
        if (v <= 0.5f)
        {
            var d = v / 0.5f;
            db = Mathf.Lerp(-80, -2, d);
        }
        else
        {
            var d = (v - 0.5f) / 0.5f;
            db = Mathf.Lerp(-2, 20, d);
        }
        _instance.GameAudioMixer.SetFloat("SeVolume", db);
    }
}
