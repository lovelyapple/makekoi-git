using UnityEngine;
using UnityEngine.UI;
using Makekoi.PartnerCreate;
public class UIPartnerClothListCell : MonoBehaviour
{
    [SerializeField] private GenderType _genderType;
    [SerializeField] private GameObject _importantMarkObj;
    public void OnReset()
    {
        _importantMarkObj.SetActive(false);
    }
    public void UpdateCell(PartnerPartData partData)
    {
        var image = GetComponent<Image>();
        image.sprite = partData.PartSprite;
        _importantMarkObj.SetActive(false);
    }
}
