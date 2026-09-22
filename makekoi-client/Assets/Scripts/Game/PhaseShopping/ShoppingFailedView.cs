using UnityEngine;
using UnityEngine.UI;
using R3;
using System;
using Cysharp.Threading.Tasks;
public class ShoppingFailedView : MonoBehaviour
{
    [SerializeField] private Button _backToTop;
    public async UniTask<Unit> OnBackToTopClicked()
    {
        await _backToTop.OnClickAsObservable().FirstAsync();
        return Unit.Default;
    }
}
