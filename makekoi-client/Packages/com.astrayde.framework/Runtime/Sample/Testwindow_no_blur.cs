using System;
using UnityEngine;
using UnityEngine.UI;
using AstraydeFramework.System.UI;
using AstraydeFramework.System;
using R3;
namespace AstraydeFramework.Sample
{
    [LocalAssetPath("Testwindow_no_blur")]
    public class Testwindow_no_blur : WindowBase
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
