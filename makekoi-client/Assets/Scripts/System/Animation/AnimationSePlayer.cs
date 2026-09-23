using UnityEngine;

/// <summary>
/// AnimationEvent から SE を鳴らすための受け口。Animator と同じ GameObject に付ける。
/// 鳴らすタイミングは演出側で決めたいので、クリップにイベントを置いて
/// String に CommonSeType の名前 (例: wallet_confirm) を入れる。
/// </summary>
public class AnimationSePlayer : MonoBehaviour
{
    /// <summary>AnimationEvent から呼ぶ。seType は CommonSeType の名前。</summary>
    public void PlaySe(string seType)
    {
        if (string.IsNullOrEmpty(seType))
        {
            Debug.LogWarning("[AnimationSePlayer] seType が空です", this);
            return;
        }

        SoundManager.PlaySeByType(seType);
    }
}
