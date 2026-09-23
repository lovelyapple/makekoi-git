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
            EnterContent(holder);
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

        // 起動直後のページ通知は Content の生成前に飛んでいるので、ここで入り直す
        EnterContent(FindHolder(_contentScrollView.CurrentIndex));
        return Unit.Default;
    }

    private UIGameContentHolder FindHolder(int pageIndex) =>
        _contentHolders.Find(holder => (int)holder.ButtonType == pageIndex);

    // ページが表示されたタイミングで、その Phase の初期化を走らせる
    private static void EnterContent(UIGameContentHolder holder)
    {
        if (holder == null || !holder.IsContentLoaded) return;
        holder.Content.EnterPhase();
    }
}
