using UnityEngine;
using UnityEngine.UI;
using R3;
using TMPro;
using System.Collections.Generic;

public class UIMoneyLotteryPanel : UICharacterSelectPanelBase
{
    [SerializeField] private Animator _rollerAnimator;
    [SerializeField] private TextMeshProUGUI _lotteryNumberText1;
    [SerializeField] private TextMeshProUGUI _lotteryNumberText2;
    [SerializeField] private TextMeshProUGUI _lotteryNumberText3;
    [SerializeField] private List<UIFrameRoller> _frameRollers;
    [SerializeField] private Button _lotteryButton;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TextMeshProUGUI _lotteryResultText;
    [SerializeField] private AnimationTriggerFinishedEvent _lotteryAnimationTriggerFinishedEvent;
    private bool _isLotteryFinished = false;
    [ContextMenu("Debug Reset Lottery")]
    public override void OnEnter()
    {
        base.OnEnter();
        _isLotteryFinished = false;
        _confirmButton.interactable = false;
        _lotteryButton.gameObject.SetActive(true);
        _rollerAnimator.Play("idle");
        _frameRollers.ForEach(roller => roller.SetState(UIFrameRoller.RollState.Rolling));
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
            }).AddTo(this);

        _lotteryAnimationTriggerFinishedEvent.OnAnimationTriggerFinishedObservable()
            .Subscribe(_ =>
            {
                _isLotteryFinished = true;
                _confirmButton.interactable = true;
                _frameRollers.ForEach(roller => roller.SetState(UIFrameRoller.RollState.Rolling));
            }).AddTo(this);
    }
    public Observable<Unit> OnConfirmObservable() =>
        _confirmButton.OnClickAsObservable();
}
