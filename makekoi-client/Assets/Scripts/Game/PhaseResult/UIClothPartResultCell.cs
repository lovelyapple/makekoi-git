using UnityEngine;
using TMPro;
public class UIClothPartResultCell : UIPartnerClothListCell
{
    [SerializeField] TextMeshProUGUI _priceLabel;
    [SerializeField] TextMeshProUGUI _costLabel;
    // 値引き率から出したこの部位のスコア。演出が進むまで隠しておく。
    [SerializeField] TextMeshProUGUI _scoreLabel;

    private const float DisplayDuration = 3f;
    private const float FadeDuration = 0.5f;

    // 全セルで表示を揃えるため、経過時間は静的に共有してフレームごとに一度だけ進める
    private static bool _perform;
    private static float _duration;
    private static int _lastTickFrame = -1;

    public void SetPartResultData(MiniGameResultData partResultData)
    {
        base.UpdateCell(partResultData.ResultPart);
        _priceLabel.text = partResultData.BasePrice.ToString();
        _costLabel.text = partResultData.CostMoney.ToString();
        if (_scoreLabel != null)
        {
            _scoreLabel.text = ShopResultScore.GetDiscountScore(partResultData).ToString();
        }
        SetLabelAlpha(_scoreLabel, 0f);
    }

    public void SetPerform(bool perform)
    {
        _perform = perform;
        _duration = 0f;

        if (!perform)
        {
            SetLabelAlpha(_priceLabel, 1f);
            SetLabelAlpha(_costLabel, 1f);
            SetLabelAlpha(_scoreLabel, 0f);
        }
    }

    private void Update()
    {
        if (!_perform) return;

        if (_lastTickFrame != Time.frameCount)
        {
            _lastTickFrame = Time.frameCount;
            _duration += Time.deltaTime;
        }

        var scoreAlpha = GetScoreAlpha(_duration % (DisplayDuration * 2f));
        SetLabelAlpha(_priceLabel, 1f - scoreAlpha);
        SetLabelAlpha(_costLabel, 1f - scoreAlpha);
        SetLabelAlpha(_scoreLabel, scoreAlpha);
    }

    private static float GetScoreAlpha(float cycleTime)
    {
        return cycleTime < DisplayDuration
            ? Mathf.InverseLerp(DisplayDuration - FadeDuration, DisplayDuration, cycleTime)
            : 1f - Mathf.InverseLerp(DisplayDuration * 2f - FadeDuration, DisplayDuration * 2f, cycleTime);
    }

    private static void SetLabelAlpha(TextMeshProUGUI label, float alpha)
    {
        if (label == null) return;

        var color = label.color;
        color.a = alpha;
        label.color = color;
    }
}
