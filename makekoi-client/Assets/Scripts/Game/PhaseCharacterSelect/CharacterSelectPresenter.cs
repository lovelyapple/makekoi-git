using UnityEngine;
using AstraydeFramework.UI;
public enum CharacterSelectPhase
{
    GenderSelect = 0,
    PartnerWish,
    MoneyLottery
}
public class CharacterSelectPresenter : GamePhaseWindowBase
{
    // snapshotを制御するクラス
    [SerializeField] private UISnapPageScrollView _characterSelectView;
    // キャラクターを選択するView
    [SerializeField] private UIGenderSelectPanel _genderSelectPanel;
    // キャラの欲しい服パーツ表示View
    [SerializeField] private UIPartnerWishPanel _partnerWishPanel;
    // 所持金を抽選するView
    [SerializeField] private UIMoneyLotteryPanel _moneyLotteryPanel;
    public override void OnEnter()
    {
        base.OnEnter();
        PartnerSelectModel.Instance.OnReset();
        _characterSelectView.GoToIndex((int)CharacterSelectPhase.GenderSelect);
    }
}
