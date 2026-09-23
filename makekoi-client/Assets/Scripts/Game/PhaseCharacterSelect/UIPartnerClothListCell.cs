using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Makekoi.PartnerCreate;
public class UIPartnerClothListCell : MonoBehaviour
{
    [SerializeField] private GenderType _genderType;
    [SerializeField] private GameObject _importantMarkObj;
    // ルートには Image が付いていないため、アイコンと名前は個別に参照する
    [SerializeField] private Image _partIconImage;
    [SerializeField] private TextMeshProUGUI _partNameText;
    public void OnReset()
    {
        _importantMarkObj.SetActive(false);
    }
    public void UpdateCell(PartnerPartData partData, bool isFavorite = false)
    {
        if (_partIconImage != null)
        {
            _partIconImage.sprite = partData.PartSprite;
            _partIconImage.enabled = partData.PartSprite != null;
        }
        if (_partNameText != null)
        {
            _partNameText.text = partData.PartMasterRecord != null ? partData.PartMasterRecord.Name : string.Empty;
        }
        // 目玉商品の印。グレードではなくお気に入り指定で点灯する
        _importantMarkObj.SetActive(isFavorite);
    }
}
