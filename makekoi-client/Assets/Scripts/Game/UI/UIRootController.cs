using UnityEngine;
using AstraydeFramework.UI;
using System;
using System.Collections.Generic;
using R3;
using Cysharp.Threading.Tasks;
public class UIRootController : MonoBehaviour
{
    [SerializeField] private UISnapPageScrollView _contentScrollView;
    [SerializeField] private UIFooterController _footerController;
    [SerializeField] private List<UIGameContentHolder> _contentHolders;
    private bool IsAllContentLoaded => _contentHolders.TrueForAll(holder => holder.IsContentLoaded);
    private void Awake()
    {
        _contentScrollView.OnPageChangedData<object>((pageIndex, data, go) =>
        {
            var holder = go.GetComponent<UIGameContentHolder>();
            _footerController.OnTypeSelected(holder.ButtonType);
        });

        InitAsync().Forget();
    }
    private async UniTask<Unit> InitAsync()
    {
        await UniTask.WaitUntil(() => IsAllContentLoaded);

        foreach (var holder in _contentHolders)
        {
            holder.OnRequestGoToNextPhase()
            .Subscribe(nextPhase => _contentScrollView.GoToIndex((int)nextPhase))
            .AddTo(this);
        }
        return Unit.Default;
    }

}
