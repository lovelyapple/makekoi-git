using UnityEngine;
using AstraydeFramework.UI;
using System;
public class UIRootController : MonoBehaviour
{
    [SerializeField] private UISnapPageScrollView _contentScrollView;
    [SerializeField] private UIFooterController _footerController;

    private void Awake()
    {
        _contentScrollView.OnPageChangedData<object>((pageIndex, data, go) =>
        {
            var holder = go.GetComponent<UIGameContentHolder>();
            _footerController.OnTypeSelected(holder.ButtonType);
        });
    }

}
