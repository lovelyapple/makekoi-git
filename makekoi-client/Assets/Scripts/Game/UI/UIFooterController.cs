using System.Collections.Generic;
using UnityEngine;

public class UIFooterController : MonoBehaviour
{
    [SerializeField] private List<UIFooterButton> _footerButtons;
    private void Awake()
    {
        foreach (var button in _footerButtons)
        {
            button.OnTypeSelected(GamePhaseContentType.Title);
        }
    }
    public void OnTypeSelected(GamePhaseContentType contentType)
    {
        foreach (var button in _footerButtons)
        {
            button.OnTypeSelected(contentType);
        }
    }
}
