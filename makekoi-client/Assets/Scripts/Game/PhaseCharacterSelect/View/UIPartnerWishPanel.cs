using System.Collections.Generic;
using UnityEngine;
public class UIPartnerWishPanel : UICharacterSelectPanelBase
{
    [SerializeField] private List<UIPartnerClothListCell> _wishListCells;
    public override void OnEnter()
    {
        _wishListCells.ForEach(cell => cell.OnReset());
    }
}
