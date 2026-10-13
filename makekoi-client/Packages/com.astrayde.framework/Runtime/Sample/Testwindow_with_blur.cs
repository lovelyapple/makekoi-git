using UnityEngine;
using AstraydeFramework.System.UI;
using AstraydeFramework.System;
using UnityEngine.UI;
using R3;
namespace AstraydeFramework.Sample
{
    [LocalAssetPath("Testwindow_with_blur")]
    public class Testwindow_with_blur : WindowBase
    {
        [SerializeField] private Button CloseButton;
        private void Awake()
        {
            CloseButton.OnClickAsObservable()
            .Subscribe(_ => OnClickCose())
            .AddTo(this);
        }
        private void OnClickCose()
        {
            this.Close();
        }
    }
}
