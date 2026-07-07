using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;
public class UIPartnerWishPanel : UICharacterSelectPanelBase
{
    [SerializeField] private List<UIPartnerClothListCell> _wishListCells;
    [SerializeField] private Button _confirmButton;
    public Observable<Unit> OnConfirmObservable() =>
        _confirmButton.OnClickAsObservable();
    public override void OnEnter()
    {
        _wishListCells.ForEach(cell => cell.OnReset());
    }
}
