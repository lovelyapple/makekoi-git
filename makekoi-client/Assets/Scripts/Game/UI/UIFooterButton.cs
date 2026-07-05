using UnityEngine;
using UnityEngine.UI;
public enum FooterButtonType
{
    Title,
    CharaSelect,
    Shopping,
    Result
}
public class UIFooterButton : MonoBehaviour
{
    [SerializeField] private FooterButtonType _buttonType;
    [SerializeField] private Image _selectedBgObject;
    [SerializeField] private GameObject _selectedTextObject;
    [SerializeField] private GameObject _unselectedTextObject;
    public void OnTypeSelected(FooterButtonType buttonType)
    {
        _selectedBgObject.enabled = _buttonType == buttonType;
        _selectedTextObject.SetActive(_buttonType == buttonType);
        _unselectedTextObject.SetActive(_buttonType != buttonType);
    }
}
