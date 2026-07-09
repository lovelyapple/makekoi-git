using UnityEngine;
using UnityEngine.UI;
using R3;
using Makekoi.PartnerCreate;
public class UIGenderSelectButton : MonoBehaviour
{
    [SerializeField] private GenderType _genderType;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _unselectedImageObj;
    [SerializeField] private Image _characterImage;
    public Sprite CharacterSprite => _characterImage.sprite;
    public Observable<GenderType> OnClickObserable() => _button.OnClickAsObservable().Select(_ => _genderType);
    public void OnReset()
    {
        _unselectedImageObj.SetActive(true);
    }
    public void OnAnySelected(GenderType selectedGenderType)
    {
        _unselectedImageObj.SetActive(_genderType != selectedGenderType);
    }
}
