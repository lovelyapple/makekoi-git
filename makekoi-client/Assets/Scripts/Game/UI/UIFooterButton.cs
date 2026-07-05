using UnityEngine;
using UnityEngine.UI;
public enum GamePhaseContentType
{
    Title,
    CharaSelect,
    Shopping,
    Result
}
public class UIFooterButton : MonoBehaviour
{
    [SerializeField] private GamePhaseContentType _buttonType;
    [SerializeField] private Image _selectedBgObject;
    [SerializeField] private GameObject _selectedTextObject;
    [SerializeField] private GameObject _unselectedTextObject;
    public void OnTypeSelected(GamePhaseContentType buttonType)
    {
        _selectedBgObject.enabled = _buttonType == buttonType;
        _selectedTextObject.SetActive(_buttonType == buttonType);
        _unselectedTextObject.SetActive(_buttonType != buttonType);
    }
}
