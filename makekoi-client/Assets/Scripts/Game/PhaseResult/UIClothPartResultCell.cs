using UnityEngine;
using TMPro;
public class UIClothPartResultCell : UIPartnerClothListCell
{
    [SerializeField] TextMeshProUGUI _priceLabel;
    [SerializeField] TextMeshProUGUI _costLabel;
    // 値引き率から出したこの部位のスコア。演出が進むまで隠しておく。
    [SerializeField] GameObject _scoreRoot;
    [SerializeField] TextMeshProUGUI _scoreLabel;
    public void SetPartResultData(MiniGameResultData partResultData)
    {
        base.UpdateCell(partResultData.ResultPart);
        _priceLabel.text = partResultData.BasePrice.ToString();
        _costLabel.text = partResultData.CostMoney.ToString();
        if (_scoreLabel != null)
        {
            _scoreLabel.text = ShopResultScore.GetDiscountScore(partResultData).ToString();
        }
        SetPerform(false);
    }
    /// <summary>値段を見せたあとに、この部位のスコアを出す。</summary>
    public void SetPerform(bool isScoreVisible)
    {
        if (_scoreRoot != null)
        {
            _scoreRoot.SetActive(isScoreVisible);
        }
    }
}
