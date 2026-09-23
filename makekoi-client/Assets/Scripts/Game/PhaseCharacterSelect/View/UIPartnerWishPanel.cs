using System.Collections.Generic;
using Makekoi.PartnerCreate;
using R3;
using UnityEngine;
using UnityEngine.UI;
public class UIPartnerWishPanel : UICharacterSelectPanelBase
{
    [SerializeField] private List<UIPartnerClothListCell> _wishListCells;
    [SerializeField] private Button _confirmButton;
    public Observable<Unit> OnConfirmObservable() =>
        _confirmButton.OnClickAsObservable();
    // キャラ確定時に抽選した「今日の品揃え」を並べる。部位数より多いセルは伏せる。
    public override void OnEnter()
    {
        var offeredParts = ShoppingModel.Instance.OfferedParts;
        var cellIndex = 0;

        foreach (PartType partType in System.Enum.GetValues(typeof(PartType)))
        {
            if (cellIndex >= _wishListCells.Count) break;
            if (!offeredParts.TryGetValue(partType, out var part) || part == null) continue;

            var cell = _wishListCells[cellIndex];
            cell.gameObject.SetActive(true);
            cell.OnReset();
            cell.UpdateCell(part, ShoppingModel.Instance.IsFavorite(partType));
            cellIndex++;
        }

        for (; cellIndex < _wishListCells.Count; cellIndex++)
        {
            _wishListCells[cellIndex].gameObject.SetActive(false);
        }
    }
}
