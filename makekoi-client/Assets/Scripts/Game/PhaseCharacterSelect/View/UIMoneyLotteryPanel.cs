using UnityEngine;
using UnityEngine.UI;
using R3;
using TMPro;
using System;
using System.Collections.Generic;
using Makekoi.PartnerCreate;

public class UIMoneyLotteryPanel : UICharacterSelectPanelBase
{
    [SerializeField] private Animator _rollerAnimator;
    [SerializeField] private TextMeshProUGUI _lotteryNumberText1;
    [SerializeField] private TextMeshProUGUI _lotteryNumberText2;
    [SerializeField] private TextMeshProUGUI _lotteryNumberText3;
    [SerializeField] private GameObject _bonusFrame1;
    [SerializeField] private GameObject _bonusFrame2;
    [SerializeField] private GameObject _bonusFrame3;
    [SerializeField] private List<UIFrameRoller> _frameRollers;
    [SerializeField] private Button _lotteryButton;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TextMeshProUGUI _lotteryResultText;
    [SerializeField] private AnimationTriggerFinishedEvent _lotteryAnimationTriggerFinishedEvent;
    private bool _isLotteryFinished = false;
    private readonly bool[] _bonusDigits = new bool[LotteryDigitCount];

    private const int LotteryDigitCount = 3;
    private const int MinLotteryNumber = 1;
    private const int MaxLotteryNumber = 7;
    private const int JackpotNumber = 7;
    private const float BaseMoneyRate = 0.6f;
    private const float NormalFluctuationRate = 0.1f;
    private const float JackpotBonusRate = 1f;
    private const float TripleBonusRate = 0.35f;
    private const float PairBonusRate = 0.15f;
    [ContextMenu("Debug Reset Lottery")]
    public override void OnEnter()
    {
        base.OnEnter();
        _isLotteryFinished = false;
        _confirmButton.interactable = false;
        _lotteryButton.gameObject.SetActive(true);
        _rollerAnimator.Play("idle");
        _frameRollers.ForEach(roller => roller.SetState(UIFrameRoller.RollState.Rolling));
        HideBonusFrames();
    }
    private void Awake()
    {
        _lotteryButton.OnClickAsObservable()
            .Where(_ => !_isLotteryFinished)
            .Subscribe(_ =>
            {
                _isLotteryFinished = true;
                _lotteryButton.gameObject.SetActive(false);
                _rollerAnimator.Play("vallet_roll_perform_start");
                _frameRollers.ForEach(roller => roller.SetState(UIFrameRoller.RollState.MaxSpeed));
                HideBonusFrames();
                LotteryNumber();
            }).AddTo(this);

        _lotteryAnimationTriggerFinishedEvent.OnAnimationTriggerFinishedObservable()
            .Subscribe(_ =>
            {
                _isLotteryFinished = true;
                _confirmButton.interactable = true;
                _frameRollers.ForEach(roller => roller.SetState(UIFrameRoller.RollState.Rolling));
                _bonusFrame1.SetActive(_bonusDigits[0]);
                _bonusFrame2.SetActive(_bonusDigits[1]);
                _bonusFrame3.SetActive(_bonusDigits[2]);
                _lotteryResultText.text = _money.ToString();
            }).AddTo(this);
    }
    public Observable<Unit> OnConfirmObservable() =>
        _confirmButton.OnClickAsObservable();
    private void LotteryNumber()
    {
        var numbers = new int[LotteryDigitCount];
        for (var index = 0; index < numbers.Length; index++)
        {
            numbers[index] = UnityEngine.Random.Range(MinLotteryNumber, MaxLotteryNumber + 1);
        }

        _lotteryNumberText1.text = numbers[0].ToString();
        _lotteryNumberText2.text = numbers[1].ToString();
        _lotteryNumberText3.text = numbers[2].ToString();

        var bonusRate = GetBonusRate(numbers);
        UpdateBonusDigits(numbers);
        var baseMoney = CalcBaseMoney();
        _money = Mathf.RoundToInt(
            baseMoney * UnityEngine.Random.Range(1f - NormalFluctuationRate, 1f + NormalFluctuationRate) * (1f + bonusRate));

        GameModel.Instance.UpdateMoney(_money);
        _lotteryResultText.text = "--";
    }
    private int _money;

    // その日並ぶ品揃えの定価合計に基準額の割合を掛けた額
    private static int CalcBaseMoney()
    {
        var priceTotal = 0;
        foreach (var offeredPart in ShoppingModel.Instance.OfferedParts.Values)
        {
            if (offeredPart == null) continue;

            priceTotal += offeredPart.Price;
        }

        return Mathf.RoundToInt(priceTotal * BaseMoneyRate);
    }

    // ボーナスは条件を満たすもののうち最も高い一種類だけ適用する
    private static float GetBonusRate(IReadOnlyList<int> numbers)
    {
        var isAllSame = numbers[0] == numbers[1] && numbers[1] == numbers[2];
        if (isAllSame && numbers[0] == JackpotNumber) return JackpotBonusRate;
        if (isAllSame) return TripleBonusRate;

        var hasPair = numbers[0] == numbers[1] || numbers[1] == numbers[2] || numbers[0] == numbers[2];
        return hasPair ? PairBonusRate : 0f;
    }

    // ボーナス成立に絡んだ桁だけ枠を出す
    private void UpdateBonusDigits(IReadOnlyList<int> numbers)
    {
        if (numbers[0] == numbers[1] && numbers[1] == numbers[2])
        {
            _bonusDigits[0] = true;
            _bonusDigits[1] = true;
            _bonusDigits[2] = true;
            return;
        }

        _bonusDigits[0] = numbers[0] == numbers[1] || numbers[0] == numbers[2];
        _bonusDigits[1] = numbers[1] == numbers[0] || numbers[1] == numbers[2];
        _bonusDigits[2] = numbers[2] == numbers[0] || numbers[2] == numbers[1];
    }

    private void HideBonusFrames()
    {
        for (var index = 0; index < _bonusDigits.Length; index++)
        {
            _bonusDigits[index] = false;
        }

        _bonusFrame1.SetActive(false);
        _bonusFrame2.SetActive(false);
        _bonusFrame3.SetActive(false);
    }
}
