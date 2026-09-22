using R3;
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
    public override GamePhaseContentType ThisPhaseContentType => GamePhaseContentType.CharaSelect;
    private void Awake()
    {
        _characterSelectView.OnPageChangedData<object>((pageIndex, data, go) =>
        {
            var panelBase = go.GetComponent<UICharacterSelectPanelBase>();
            panelBase.OnEnter();
        });

        _genderSelectPanel.OnConfirmObserable()
            .Subscribe(genderType =>
            {
                GameModel.Instance.UpdateGenderType(genderType);
                // キャラが決まった時点で、この先の買い物で並ぶ品をここで抽選しておく
                ShoppingModel.Instance.Init(genderType);
                ShoppingModel.Instance.StartShopping();
                _characterSelectView.GoToIndex((int)CharacterSelectPhase.PartnerWish);
            }).AddTo(this);

        _partnerWishPanel.OnConfirmObservable()
            .Subscribe(_ =>
            {
                _characterSelectView.GoToIndex((int)CharacterSelectPhase.MoneyLottery);
            }).AddTo(this);

        _moneyLotteryPanel.OnConfirmObservable()
            .Subscribe(_ =>
            {
                RequestGoToNextPhase();
            }).AddTo(this);


        _genderSelectPanel.OnEnter();
    }
    public override void OnEnter()
    {
        base.OnEnter();
        PartnerSelectModel.Instance.OnReset();
        _genderSelectPanel.OnEnter();
        _characterSelectView.GoToIndex((int)CharacterSelectPhase.GenderSelect);
    }
}
