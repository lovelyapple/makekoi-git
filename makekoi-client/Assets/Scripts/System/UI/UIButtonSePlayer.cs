using R3;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSePlayer : MonoBehaviour
{
    [SerializeField] CommonSeType SelectSeType = CommonSeType.common_btn_confirm;
    [SerializeField] AudioClip AudioClip;

    private void Awake()
    {
        GetComponent<Button>().OnClickAsObservable()
            .Subscribe(_ =>
            {
                if (AudioClip == null)
                {
                    AudioClip = SoundManager.GetCommonSeByType(SelectSeType);
                }

                SoundManager.PlaySE(AudioClip);
            }).AddTo(this);
    }
}
