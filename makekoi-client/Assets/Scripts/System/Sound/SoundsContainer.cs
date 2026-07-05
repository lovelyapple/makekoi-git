using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class SoundResource
{
    public string SoundType;
    public AudioClip Clip;
}

[CreateAssetMenu(fileName = "SoundsContainer", menuName = "Scriptable Objects/SoundsContainer")]
public class SoundsContainer : ScriptableObject
{
    public List<SoundResource> SeResources;
    public List<SoundResource> BgmResources;

    public AudioClip GetSeData(string seType)
    {
        var resource = SeResources.FirstOrDefault(x => x.SoundType == seType);
        if (resource == null)
        {
            Debug.LogError($"could not find se: {seType}");
            return null;
        }
        return resource.Clip;
    }

    public AudioClip GetBgmData(string bgmType)
    {
        var resource = BgmResources.FirstOrDefault(x => x.SoundType == bgmType);
        if (resource == null)
        {
            Debug.LogError($"could not find bgm: {bgmType}");
            return null;
        }
        return resource.Clip;
    }
}
